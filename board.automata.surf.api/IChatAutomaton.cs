using board.automata.surf.api.models;

namespace board.automata.surf.api
{
  public interface IChatAutomaton : IAutomaton
  {
    public uint EstimatedTokensPerSecond { get; }
    public uint ContextLimit { get; }
    public uint RequiredVRam { get; }
    public uint RequiredRam { get; }
    public bool RequiresGpu { get; }

    public OllamaGenerateResponse Generate(OllamaGenerateRequest request);

    public OllamaChatResponse Chat(OllamaChatRequest request);

    public OllamaShowResponse Show(OllamaShowRequest request);

    public OllamaStatusResponse CreateModel(OllamaCreateModelRequest request);

    public OllamaStatusResponse CopyModel(OllamaCopyModelRequest request);

    public OllamaStatusResponse DeleteModel(OllamaDeleteModelRequest request);

    public OllamaStatusResponse PullModel(OllamaPullModelRequest request);

    public OllamaStatusResponse PushModel(OllamaPushModelRequest request);

    public OllamaEmbedResponse Embed(OllamaEmbedRequest request);

    public OllamaEmbeddingsResponse Embeddings(OllamaEmbeddingsRequest request);

    public OllamaModelSummary Summary();

    public OllamaModelDetails Details();

    public OllamaPsResponse List();

    public OllamaTagsResponse Tags();

    public OllamaVersionResponse Version();
  }
}
