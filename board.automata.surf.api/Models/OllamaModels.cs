using System.Text.Json;
using System.Text.Json.Serialization;

namespace board.automata.surf.api.models;

public sealed class OllamaGenerateRequest
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("prompt")]
  public string? Prompt { get; init; }

  [JsonPropertyName("suffix")]
  public string? Suffix { get; init; }

  [JsonPropertyName("images")]
  public IReadOnlyList<string>? Images { get; init; }

  [JsonPropertyName("think")]
  public bool? Think { get; init; }

  [JsonPropertyName("format")]
  public JsonElement? Format { get; init; }

  [JsonPropertyName("options")]
  public Dictionary<string, JsonElement>? Options { get; init; }

  [JsonPropertyName("system")]
  public string? System { get; init; }

  [JsonPropertyName("template")]
  public string? Template { get; init; }

  [JsonPropertyName("stream")]
  public bool? Stream { get; init; }

  [JsonPropertyName("raw")]
  public bool? Raw { get; init; }

  [JsonPropertyName("keep_alive")]
  public JsonElement? KeepAlive { get; init; }

  [JsonPropertyName("context")]
  public IReadOnlyList<int>? Context { get; init; }

  [JsonPropertyName("width")]
  public int? Width { get; init; }

  [JsonPropertyName("height")]
  public int? Height { get; init; }

  [JsonPropertyName("steps")]
  public int? Steps { get; init; }
}

public sealed class OllamaGenerateResponse
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("created_at")]
  public DateTimeOffset? CreatedAt { get; init; }

  [JsonPropertyName("response")]
  public string? Response { get; init; }

  [JsonPropertyName("image")]
  public string? Image { get; init; }

  [JsonPropertyName("done")]
  public bool Done { get; init; }

  [JsonPropertyName("done_reason")]
  public string? DoneReason { get; init; }

  [JsonPropertyName("context")]
  public IReadOnlyList<int>? Context { get; init; }

  [JsonPropertyName("total_duration")]
  public long? TotalDuration { get; init; }

  [JsonPropertyName("load_duration")]
  public long? LoadDuration { get; init; }

  [JsonPropertyName("prompt_eval_count")]
  public int? PromptEvalCount { get; init; }

  [JsonPropertyName("prompt_eval_duration")]
  public long? PromptEvalDuration { get; init; }

  [JsonPropertyName("eval_count")]
  public int? EvalCount { get; init; }

  [JsonPropertyName("eval_duration")]
  public long? EvalDuration { get; init; }

  [JsonPropertyName("completed")]
  public long? Completed { get; init; }

  [JsonPropertyName("total")]
  public long? Total { get; init; }
}

public sealed class OllamaChatRequest
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("messages")]
  public IReadOnlyList<OllamaMessage> Messages { get; init; } = [];

  [JsonPropertyName("tools")]
  public IReadOnlyList<OllamaTool>? Tools { get; init; }

  [JsonPropertyName("think")]
  public bool? Think { get; init; }

  [JsonPropertyName("format")]
  public JsonElement? Format { get; init; }

  [JsonPropertyName("options")]
  public Dictionary<string, JsonElement>? Options { get; init; }

  [JsonPropertyName("stream")]
  public bool? Stream { get; init; }

  [JsonPropertyName("keep_alive")]
  public JsonElement? KeepAlive { get; init; }
}

public sealed class OllamaChatResponse
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("created_at")]
  public DateTimeOffset? CreatedAt { get; init; }

  [JsonPropertyName("message")]
  public OllamaMessage? Message { get; init; }

  [JsonPropertyName("done")]
  public bool Done { get; init; }

  [JsonPropertyName("done_reason")]
  public string? DoneReason { get; init; }

  [JsonPropertyName("total_duration")]
  public long? TotalDuration { get; init; }

  [JsonPropertyName("load_duration")]
  public long? LoadDuration { get; init; }

  [JsonPropertyName("prompt_eval_count")]
  public int? PromptEvalCount { get; init; }

  [JsonPropertyName("prompt_eval_duration")]
  public long? PromptEvalDuration { get; init; }

  [JsonPropertyName("eval_count")]
  public int? EvalCount { get; init; }

  [JsonPropertyName("eval_duration")]
  public long? EvalDuration { get; init; }
}

public sealed class OllamaMessage
{
  [JsonPropertyName("role")]
  public string Role { get; init; } = string.Empty;

  [JsonPropertyName("content")]
  public string? Content { get; init; }

  [JsonPropertyName("thinking")]
  public string? Thinking { get; init; }

  [JsonPropertyName("images")]
  public IReadOnlyList<string>? Images { get; init; }

  [JsonPropertyName("tool_calls")]
  public IReadOnlyList<OllamaToolCall>? ToolCalls { get; init; }

  [JsonPropertyName("tool_name")]
  public string? ToolName { get; init; }
}

public sealed class OllamaTool
{
  [JsonPropertyName("type")]
  public string Type { get; init; } = "function";

  [JsonPropertyName("function")]
  public OllamaToolDefinition Function { get; init; } = new();
}

public sealed class OllamaToolDefinition
{
  [JsonPropertyName("name")]
  public string Name { get; init; } = string.Empty;

  [JsonPropertyName("description")]
  public string? Description { get; init; }

  [JsonPropertyName("parameters")]
  public JsonElement? Parameters { get; init; }
}

public sealed class OllamaToolCall
{
  [JsonPropertyName("function")]
  public OllamaToolInvocation Function { get; init; } = new();
}

public sealed class OllamaToolInvocation
{
  [JsonPropertyName("name")]
  public string Name { get; init; } = string.Empty;

  [JsonPropertyName("arguments")]
  public JsonElement? Arguments { get; init; }
}

public sealed class OllamaCreateModelRequest
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("from")]
  public string? From { get; init; }

  [JsonPropertyName("files")]
  public Dictionary<string, string>? Files { get; init; }

  [JsonPropertyName("adapters")]
  public Dictionary<string, string>? Adapters { get; init; }

  [JsonPropertyName("template")]
  public string? Template { get; init; }

  [JsonPropertyName("license")]
  public JsonElement? License { get; init; }

  [JsonPropertyName("system")]
  public string? System { get; init; }

  [JsonPropertyName("parameters")]
  public Dictionary<string, JsonElement>? Parameters { get; init; }

  [JsonPropertyName("messages")]
  public IReadOnlyList<OllamaMessage>? Messages { get; init; }

  [JsonPropertyName("stream")]
  public bool? Stream { get; init; }

  [JsonPropertyName("quantize")]
  public string? Quantize { get; init; }
}

public sealed class OllamaShowRequest
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("verbose")]
  public bool? Verbose { get; init; }
}

public sealed class OllamaShowResponse
{
  [JsonPropertyName("modelfile")]
  public string? Modelfile { get; init; }

  [JsonPropertyName("parameters")]
  public string? Parameters { get; init; }

  [JsonPropertyName("template")]
  public string? Template { get; init; }

  [JsonPropertyName("details")]
  public OllamaModelDetails? Details { get; init; }

  [JsonPropertyName("model_info")]
  public Dictionary<string, JsonElement>? ModelInfo { get; init; }

  [JsonPropertyName("capabilities")]
  public IReadOnlyList<string>? Capabilities { get; init; }

  [JsonPropertyName("license")]
  public JsonElement? License { get; init; }

  [JsonPropertyName("system")]
  public string? System { get; init; }
}

public sealed class OllamaCopyModelRequest
{
  [JsonPropertyName("source")]
  public string Source { get; init; } = string.Empty;

  [JsonPropertyName("destination")]
  public string Destination { get; init; } = string.Empty;
}

public sealed class OllamaDeleteModelRequest
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;
}

public sealed class OllamaPullModelRequest
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("insecure")]
  public bool? Insecure { get; init; }

  [JsonPropertyName("stream")]
  public bool? Stream { get; init; }
}

public sealed class OllamaPushModelRequest
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("insecure")]
  public bool? Insecure { get; init; }

  [JsonPropertyName("stream")]
  public bool? Stream { get; init; }
}

public sealed class OllamaStatusResponse
{
  [JsonPropertyName("status")]
  public string Status { get; init; } = string.Empty;

  [JsonPropertyName("digest")]
  public string? Digest { get; init; }

  [JsonPropertyName("total")]
  public long? Total { get; init; }

  [JsonPropertyName("completed")]
  public long? Completed { get; init; }
}

public sealed class OllamaEmbedRequest
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("input")]
  public JsonElement Input { get; init; }

  [JsonPropertyName("truncate")]
  public bool? Truncate { get; init; }

  [JsonPropertyName("options")]
  public Dictionary<string, JsonElement>? Options { get; init; }

  [JsonPropertyName("keep_alive")]
  public JsonElement? KeepAlive { get; init; }

  [JsonPropertyName("dimensions")]
  public int? Dimensions { get; init; }
}

public sealed class OllamaEmbedResponse
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("embeddings")]
  public IReadOnlyList<IReadOnlyList<double>> Embeddings { get; init; } = [];

  [JsonPropertyName("total_duration")]
  public long? TotalDuration { get; init; }

  [JsonPropertyName("load_duration")]
  public long? LoadDuration { get; init; }

  [JsonPropertyName("prompt_eval_count")]
  public int? PromptEvalCount { get; init; }
}

public sealed class OllamaEmbeddingsRequest
{
  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("prompt")]
  public string Prompt { get; init; } = string.Empty;

  [JsonPropertyName("options")]
  public Dictionary<string, JsonElement>? Options { get; init; }

  [JsonPropertyName("keep_alive")]
  public JsonElement? KeepAlive { get; init; }
}

public sealed class OllamaEmbeddingsResponse
{
  [JsonPropertyName("embedding")]
  public IReadOnlyList<double> Embedding { get; init; } = [];
}

public sealed class OllamaTagsResponse
{
  [JsonPropertyName("models")]
  public IReadOnlyList<OllamaModelSummary> Models { get; init; } = [];
}

public sealed class OllamaPsResponse
{
  [JsonPropertyName("models")]
  public IReadOnlyList<OllamaModelSummary> Models { get; init; } = [];
}

public sealed class OllamaModelSummary
{
  [JsonPropertyName("name")]
  public string Name { get; init; } = string.Empty;

  [JsonPropertyName("model")]
  public string Model { get; init; } = string.Empty;

  [JsonPropertyName("modified_at")]
  public DateTimeOffset? ModifiedAt { get; init; }

  [JsonPropertyName("size")]
  public long Size { get; init; }

  [JsonPropertyName("digest")]
  public string? Digest { get; init; }

  [JsonPropertyName("details")]
  public OllamaModelDetails? Details { get; init; }

  [JsonPropertyName("expires_at")]
  public DateTimeOffset? ExpiresAt { get; init; }

  [JsonPropertyName("size_vram")]
  public long? SizeVram { get; init; }
}

public sealed class OllamaModelDetails
{
  [JsonPropertyName("parent_model")]
  public string? ParentModel { get; init; }

  [JsonPropertyName("format")]
  public string? Format { get; init; }

  [JsonPropertyName("family")]
  public string? Family { get; init; }

  [JsonPropertyName("families")]
  public IReadOnlyList<string>? Families { get; init; }

  [JsonPropertyName("parameter_size")]
  public string? ParameterSize { get; init; }

  [JsonPropertyName("quantization_level")]
  public string? QuantizationLevel { get; init; }
}

public sealed class OllamaVersionResponse
{
  [JsonPropertyName("version")]
  public string Version { get; init; } = string.Empty;
}
