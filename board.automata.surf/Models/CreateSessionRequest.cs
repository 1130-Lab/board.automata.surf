namespace board.automata.surf.models;

public sealed class CreateSessionRequest
{
    public string ModelName { get; init; } = "default";
    public string PublicKey { get; init; } = "default";
    public string ClientAddress { get; init; } = "127.0.0.1";
    public int ClientPort { get; init; } = 1337;
}
