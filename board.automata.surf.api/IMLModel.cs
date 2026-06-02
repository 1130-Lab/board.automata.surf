namespace board.automata.surf.api
{
  public interface IMLModel
  {
    public string Name { get; }
    public string Description { get; }
    public string Version { get; }
    public string Url { get; }
    public string LocalPath { get; }
    public string MaxInputSize { get; } // Tokens, resolution, audio/video length
    public string MaxOutputSize { get; }
    public string EstimatedOutputRate { get; } // Tokens-per-second, image/audio/video gen time

    public string Prompt(string prompt);
  }
}
