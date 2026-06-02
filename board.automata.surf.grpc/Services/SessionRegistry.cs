using System.Collections.Concurrent;

namespace board.automata.surf.grpc.services;

public sealed class PreparedSessionRegistry
{
  private readonly Dictionary<string, PreparedSession> _preparedSessions = new(StringComparer.Ordinal);
  private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

  public async Task PrepareAsync(string sessionId, CancellationToken? ct)
  {
    await _lock.WaitAsync(ct ?? CancellationToken.None);
    try
    {
      _preparedSessions[sessionId] = new PreparedSession(sessionId, DateTimeOffset.UtcNow);
    }
    finally
    {
      _lock.Release();
    }
  }

  public async Task<bool> IsPreparedAsync(string sessionId, CancellationToken? ct)
  {
    await _lock.WaitAsync(ct ?? CancellationToken.None);
    try
    {
      return _preparedSessions.ContainsKey(sessionId);
    }
    finally
    {
      _lock.Release();
    }
  }

  public async Task Complete(string sessionId,  CancellationToken? ct)
  {
    await _lock.WaitAsync(ct ?? CancellationToken.None);
    try
    {
      _preparedSessions.Remove(sessionId, out _);
    }
    finally
    {
      _lock.Release();
    }
  } 
}

public sealed record PreparedSession(
    string SessionId,
    DateTimeOffset TimestampUtc
);
