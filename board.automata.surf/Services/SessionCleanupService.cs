namespace board.automata.surf.services;

public sealed class SessionCleanupService : BackgroundService
{
    private readonly SessionStore _sessionStore;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(SessionStore sessionStore, ILogger<SessionCleanupService> logger)
    {
        _sessionStore = sessionStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var removed = _sessionStore.CleanupExpired();
            if (removed.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired sessions", removed.Count);
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
