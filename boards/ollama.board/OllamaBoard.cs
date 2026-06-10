using board.automata.surf.api;
using board.automata.surf.api.models;

namespace board.automata.surf.boards.ollama;

public sealed class OllamaBoard(OllamaClient ollama, string address, string model) : IChatAutomaton
{
  public string BoardName => "Ollama Board";
  public string BoardDescription => "Runs models locally via Ollama, forwarding requests to board.automata.surf";
  public string BoardVersion => "1.0.0";
  public string BoardUrl => "https://board.automata.surf/boards/ollama-board";
  public string BoardAddress => address;

  public uint EstimatedTokensPerSecond => 15;
  public uint ContextLimit => 131072;
  public uint RequiredVRam => 10240;
  public uint RequiredRam => 16384;
  public bool RequiresGpu => false;

  private readonly OllamaClient _ollama = ollama;
  private readonly string _defaultModel = model;

  public async Task<OllamaGenerateResponse> Generate(OllamaGenerateRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return await _ollama.GenerateAsync(request)
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public async Task<OllamaChatResponse> Chat(OllamaChatRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return await _ollama.ChatAsync(request)
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public async Task<OllamaShowResponse> Show(OllamaShowRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return await _ollama.ShowAsync(request)
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public async Task<OllamaStatusResponse> CreateModel(OllamaCreateModelRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return await _ollama.CreateModelAsync(request)
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public async Task<OllamaStatusResponse> CopyModel(OllamaCopyModelRequest request)
  {
    return await _ollama.CopyModelAsync(request)
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public async Task<OllamaStatusResponse> DeleteModel(OllamaDeleteModelRequest request)
  {
    return await _ollama.DeleteModelAsync(request)
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public async Task<OllamaStatusResponse> PullModel(OllamaPullModelRequest request)
  {
    return await _ollama.PullModelAsync(request)
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public async Task<OllamaStatusResponse> PushModel(OllamaPushModelRequest request)
  {
    return await _ollama.PushModelAsync(request)
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public async Task<OllamaEmbedResponse> Embed(OllamaEmbedRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return await _ollama.EmbedAsync(request)
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public async Task<OllamaEmbeddingsResponse> Embeddings(OllamaEmbeddingsRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return await _ollama.EmbeddingsAsync(request)
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public async Task<OllamaModelSummary> Summary()
  {
    var tags = await _ollama.TagsAsync();
    return tags?.Models?.FirstOrDefault(m => m.Name.Contains("gemma4"))
           ?? new OllamaModelSummary { Name = _defaultModel, Model = _defaultModel };
  }

  public async Task<OllamaModelDetails> Details()
  {
    return await Task.FromResult(new OllamaModelDetails
    {
      Family = "gemma4",
      Families = ["gemma", "llama"],
      ParameterSize = "4.5B effective (8B with embeddings)",
      QuantizationLevel = "Q4_K_M",
      Format = "gguf"
    });
  }

  public async Task<OllamaPsResponse> List()
  {
    return await _ollama.PsAsync()
           ?? new OllamaPsResponse();
  }

  public async Task<OllamaTagsResponse> Tags()
  {
    return await _ollama.TagsAsync()
           ?? new OllamaTagsResponse();
  }

  public async Task<OllamaVersionResponse> Version()
  {
    return await _ollama.VersionAsync()
           ?? new OllamaVersionResponse { Version = "unknown" };
  }

  private string ResolveModel(string? model)
  {
    return string.IsNullOrWhiteSpace(model) || model == "default" ? _defaultModel : model;
  }

  public async Task<bool> EnsureModelAvailableAsync(string? model = null, CancellationToken ct = default)
  {
    model = ResolveModel(model);
    var tags = await _ollama.TagsAsync(ct);
    if (tags?.Models?.Any(m => m.Name == model || m.Model == model) == true)
      return true;

    Console.WriteLine($"Pulling {model}...");
    var result = await _ollama.PullModelAsync(new OllamaPullModelRequest { Model = model }, ct);
    return result?.Status == "success";
  }
}
