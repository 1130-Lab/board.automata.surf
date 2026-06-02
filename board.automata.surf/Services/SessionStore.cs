using Microsoft.Extensions.Options;

namespace board.automata.surf.services;

public sealed class SessionStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, SessionRecord> _sessions = new(StringComparer.Ordinal);
    private readonly TimeSpan _reservationTtl;
    private readonly TimeSpan _inactivityTimeout;

    public SessionStore(IOptions<SessionStoreOptions> options)
    {
        var settings = options.Value;
        _reservationTtl = TimeSpan.FromSeconds(settings.ReservationTtlSeconds);
        _inactivityTimeout = TimeSpan.FromSeconds(settings.InactivityTimeoutSeconds);
    }

    public SessionRecord CreateReservedSession(string sessionId, string modelName, string modelEndpoint)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new SessionRecord(
            SessionId: sessionId,
            ModelName: modelName,
            ModelEndpoint: modelEndpoint,
            State: SessionState.Reserved,
            CreatedAtUtc: now,
            LastActivityUtc: now,
            ReservedUntilUtc: now.Add(_reservationTtl),
            TokenJti: null,
            TokenExpiresAtUtc: null,
            CloseReason: null
        );

        lock (_gate)
        {
            _sessions[session.SessionId] = session;
        }

        return session;
    }

    public bool MarkActive(string sessionId, string tokenJti, DateTimeOffset tokenExpiresAtUtc)
    {
        lock (_gate)
        {
            if (!_sessions.TryGetValue(sessionId, out var existing))
            {
                return false;
            }

            if (existing.State != SessionState.Reserved || DateTimeOffset.UtcNow > existing.ReservedUntilUtc)
            {
                return false;
            }

            _sessions[sessionId] = existing with
            {
                State = SessionState.Active,
                LastActivityUtc = DateTimeOffset.UtcNow,
                TokenJti = tokenJti,
                TokenExpiresAtUtc = tokenExpiresAtUtc
            };

            return true;
        }
    }

    public bool TryGetActiveSession(string sessionId, out SessionRecord session)
    {
        lock (_gate)
        {
            if (!_sessions.TryGetValue(sessionId, out var existing) || existing.State != SessionState.Active)
            {
                session = default!;
                return false;
            }

            if (IsExpired(existing, DateTimeOffset.UtcNow))
            {
                _sessions.Remove(sessionId);
                session = default!;
                return false;
            }

            session = existing;
            return true;
        }
    }

    public void Touch(string sessionId)
    {
        lock (_gate)
        {
            if (!_sessions.TryGetValue(sessionId, out var existing) || existing.State != SessionState.Active)
            {
                return;
            }

            _sessions[sessionId] = existing with { LastActivityUtc = DateTimeOffset.UtcNow };
        }
    }

    public void MarkFailed(string sessionId, string reason)
    {
        lock (_gate)
        {
            if (!_sessions.TryGetValue(sessionId, out var existing))
            {
                return;
            }

            _sessions[sessionId] = existing with
            {
                State = SessionState.Failed,
                CloseReason = reason,
                LastActivityUtc = DateTimeOffset.UtcNow
            };
        }
    }

    public void Close(string sessionId, string reason)
    {
        lock (_gate)
        {
            if (!_sessions.TryGetValue(sessionId, out var existing))
            {
                return;
            }

            _sessions[sessionId] = existing with
            {
                State = SessionState.Closed,
                CloseReason = reason,
                LastActivityUtc = DateTimeOffset.UtcNow
            };
        }
    }

    public IReadOnlyCollection<SessionRecord> CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        List<SessionRecord> removed = [];

        lock (_gate)
        {
            foreach (var item in _sessions.Values.ToList())
            {
                var isTerminal = item.State is SessionState.Closed or SessionState.Failed;
                if (!isTerminal && !IsExpired(item, now))
                {
                    continue;
                }

                _sessions.Remove(item.SessionId);
                removed.Add(item);
            }
        }

        return removed;
    }

    private bool IsExpired(SessionRecord session, DateTimeOffset now)
    {
        if (session.State == SessionState.Reserved && now > session.ReservedUntilUtc)
        {
            return true;
        }

        return session.State == SessionState.Active && (now - session.LastActivityUtc) > _inactivityTimeout;
    }
}

public sealed class SessionStoreOptions
{
    public int ReservationTtlSeconds { get; set; } = 30;
    public int InactivityTimeoutSeconds { get; set; } = 300;
}

public enum SessionState
{
    Reserved = 0,
    Active = 1,
    Closed = 2,
    Failed = 3
}

public sealed record SessionRecord(
    string SessionId,
    string ModelName,
    string ModelEndpoint,
    SessionState State,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastActivityUtc,
    DateTimeOffset ReservedUntilUtc,
    string? TokenJti,
    DateTimeOffset? TokenExpiresAtUtc,
    string? CloseReason
);
