namespace board.automata.surf.services;

public sealed class ModelBridgeOptions
{
  public string GrpcEndpoint { get; set; } = "http://localhost:50061";
  public int PrepareSessionTimeoutSeconds { get; set; } = 10;
  public string WebSocketPath { get; set; } = "/ws";
  public string CompressionFormat { get; set; } = "gzip";
}
