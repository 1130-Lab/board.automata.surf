namespace board.automata.surf.boards.ollama;

public sealed class OllamaBoardConfiguration
{
  public required List<string> AllowedModels { get; set; }
  public required List<string> DisallowedModels { get; set; }
  public required string DefaultModel { get; set; }
  public int MaxUsers { get; }
}
