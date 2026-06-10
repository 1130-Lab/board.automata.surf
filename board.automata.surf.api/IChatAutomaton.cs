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

    public Task<OllamaGenerateResponse> Generate(OllamaGenerateRequest request);

    public Task<OllamaChatResponse> Chat(OllamaChatRequest request);

    public Task<OllamaShowResponse> Show(OllamaShowRequest request);

    public Task<OllamaStatusResponse> CreateModel(OllamaCreateModelRequest request);

    public Task<OllamaStatusResponse> CopyModel(OllamaCopyModelRequest request);

    public Task<OllamaStatusResponse> DeleteModel(OllamaDeleteModelRequest request);

    public Task<OllamaStatusResponse> PullModel(OllamaPullModelRequest request);

    public Task<OllamaStatusResponse> PushModel(OllamaPushModelRequest request);

    public Task<OllamaEmbedResponse> Embed(OllamaEmbedRequest request);

    public Task<OllamaEmbeddingsResponse> Embeddings(OllamaEmbeddingsRequest request);

    public Task<OllamaModelSummary> Summary();

    public Task<OllamaModelDetails> Details();

    public Task<OllamaPsResponse> List();

    public Task<OllamaTagsResponse> Tags();

    public Task<OllamaVersionResponse> Version();
  }
}
