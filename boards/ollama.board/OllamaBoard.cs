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

  public OllamaGenerateResponse Generate(OllamaGenerateRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return _ollama.GenerateAsync(request).GetAwaiter().GetResult()
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public OllamaChatResponse Chat(OllamaChatRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return _ollama.ChatAsync(request).GetAwaiter().GetResult()
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public OllamaShowResponse Show(OllamaShowRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return _ollama.ShowAsync(request).GetAwaiter().GetResult()
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public OllamaStatusResponse CreateModel(OllamaCreateModelRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return _ollama.CreateModelAsync(request).GetAwaiter().GetResult()
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public OllamaStatusResponse CopyModel(OllamaCopyModelRequest request)
  {
    return _ollama.CopyModelAsync(request).GetAwaiter().GetResult()
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public OllamaStatusResponse DeleteModel(OllamaDeleteModelRequest request)
  {
    return _ollama.DeleteModelAsync(request).GetAwaiter().GetResult()
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public OllamaStatusResponse PullModel(OllamaPullModelRequest request)
  {
    return _ollama.PullModelAsync(request).GetAwaiter().GetResult()
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public OllamaStatusResponse PushModel(OllamaPushModelRequest request)
  {
    return _ollama.PushModelAsync(request).GetAwaiter().GetResult()
           ?? new OllamaStatusResponse { Status = "error" };
  }

  public OllamaEmbedResponse Embed(OllamaEmbedRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return _ollama.EmbedAsync(request).GetAwaiter().GetResult()
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public OllamaEmbeddingsResponse Embeddings(OllamaEmbeddingsRequest request)
  {
    request.Model = ResolveModel(request.Model);
    return _ollama.EmbeddingsAsync(request).GetAwaiter().GetResult()
           ?? throw new InvalidOperationException("Ollama returned no response.");
  }

  public OllamaModelSummary Summary()
  {
    var tags = _ollama.TagsAsync().GetAwaiter().GetResult();
    return tags?.Models?.FirstOrDefault(m => m.Name.Contains("gemma4"))
           ?? new OllamaModelSummary { Name = _defaultModel, Model = _defaultModel };
  }

  public OllamaModelDetails Details()
  {
    return new OllamaModelDetails
    {
      Family = "gemma4",
      Families = ["gemma", "llama"],
      ParameterSize = "4.5B effective (8B with embeddings)",
      QuantizationLevel = "Q4_K_M",
      Format = "gguf"
    };
  }

  public OllamaPsResponse List()
  {
    return _ollama.PsAsync().GetAwaiter().GetResult()
           ?? new OllamaPsResponse();
  }

  public OllamaTagsResponse Tags()
  {
    return _ollama.TagsAsync().GetAwaiter().GetResult()
           ?? new OllamaTagsResponse();
  }

  public OllamaVersionResponse Version()
  {
    return _ollama.VersionAsync().GetAwaiter().GetResult()
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
