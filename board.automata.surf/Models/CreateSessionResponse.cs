namespace board.automata.surf.models;

public sealed class CreateSessionResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string WebSocketUrl { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; init; }
}
