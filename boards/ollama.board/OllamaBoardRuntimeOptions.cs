namespace board.automata.surf.boards.ollama;

public sealed class OllamaBoardRuntimeOptions
{
  public string DefaultModel { get; init; } = "gemma4:e4b";
  public string CompressionFormat { get; init; } = "gzip";
  public string ModelKeepAlive { get; init; } = "-1";
}
