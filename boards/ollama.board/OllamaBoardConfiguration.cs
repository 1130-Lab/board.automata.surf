namespace board.automata.surf.boards.ollama;

public sealed class OllamaBoardConfiguration
{
  public List<string> AllowedModels { get; }
  public List<string> DisallowedModels { get; }
  public string DefaultModel { get; }
  public int MaxUsers { get; }
}
