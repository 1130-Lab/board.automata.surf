using System.Text;
using System.Text.Json;
using board.automata.surf.api;
using board.automata.surf.api.models;
using board.automata.surf.proto;
using Grpc.Core;

namespace board.automata.surf.boards.ollama;

public sealed class OllamaBoardGrpcService : ModelSurfness.ModelSurfnessBase
{
  private readonly OllamaClient _ollama;
  private readonly OllamaBoardRuntimeOptions _options;
  private readonly JsonElement _keepAlive;
  private readonly ILogger<OllamaBoardGrpcService> _logger;
  private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

  public OllamaBoardGrpcService(
      OllamaClient ollama,
      OllamaBoardRuntimeOptions options,
      ILogger<OllamaBoardGrpcService> logger)
  {
    _ollama = ollama;
    _options = options;
    _keepAlive = ParseKeepAlive(options.ModelKeepAlive);
    _logger = logger;
  }

  public override async Task ExchangeMessages(
      IAsyncStreamReader<ClientMessage> requestStream,
      IServerStreamWriter<ServerMessage> responseStream,
      ServerCallContext context)
  {
    while (await requestStream.MoveNext(context.CancellationToken))
    {
      await ProcessRequestAsync(requestStream.Current, responseStream, context.CancellationToken);
    }
  }

  private async Task ProcessRequestAsync(
      ClientMessage request,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    if (request.LlmMessage is null)
    {
      await WriteErrorAsync(request, request.LlmMessageType, "Missing llm_message payload.", responseStream, cancellationToken);
      return;
    }

    var endpoint = NormalizeEndpoint(request.Endpoint);
    var endpointKey = endpoint.ToLowerInvariant();

    try
    {
      var payload = CompressedPayloadCodec.Decompress(
        request.LlmMessage.CompressedData.ToByteArray(),
        request.LlmMessage.CompressionFormat);

      if (endpointKey.StartsWith("blobs/head/", StringComparison.Ordinal))
      {
        await ProcessBlobHeadAsync(request, endpoint["blobs/head/".Length..], responseStream, cancellationToken);
        return;
      }

      if (endpointKey.StartsWith("blobs/post/", StringComparison.Ordinal))
      {
        await ProcessBlobPostAsync(request, endpoint["blobs/post/".Length..], payload, responseStream, cancellationToken);
        return;
      }

      var requestJson = GetJsonPayload(payload);
      switch (endpointKey)
      {
        case "chat":
          await ProcessChatAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "generate":
          await ProcessGenerateAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "create":
          await ProcessCreateAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "show":
          await ProcessShowAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "copy":
          await ProcessCopyAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "delete":
          await ProcessDeleteAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "pull":
          await ProcessPullAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "push":
          await ProcessPushAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "embed":
          await ProcessEmbedAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "embeddings":
          await ProcessEmbeddingsAsync(request, requestJson, responseStream, cancellationToken);
          return;
        case "tags":
          await ProcessTagsAsync(request, responseStream, cancellationToken);
          return;
        case "ps":
          await ProcessPsAsync(request, responseStream, cancellationToken);
          return;
        case "version":
          await ProcessVersionAsync(request, responseStream, cancellationToken);
          return;
      }

      await ProcessLegacyInferenceAsync(request, requestJson, responseStream, cancellationToken);
    }
    catch (JsonException ex)
    {
      _logger.LogWarning(ex, "Invalid JSON payload for session {SessionId}", request.Header?.SessionId);
      await WriteErrorAsync(request, request.LlmMessageType, "Invalid JSON payload.", responseStream, cancellationToken);
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Invalid inference payload for session {SessionId}", request.Header?.SessionId);
      await WriteErrorAsync(request, request.LlmMessageType, ex.Message, responseStream, cancellationToken);
    }
    catch (InvalidDataException ex)
    {
      _logger.LogWarning(ex, "Invalid compressed payload for session {SessionId}", request.Header?.SessionId);
      await WriteErrorAsync(request, request.LlmMessageType, "Invalid compressed payload.", responseStream, cancellationToken);
    }
    catch (HttpRequestException ex)
    {
      _logger.LogError(ex, "Failed to call Ollama for session {SessionId}", request.Header?.SessionId);
      await WriteErrorAsync(request, request.LlmMessageType, "Failed to reach Ollama.", responseStream, cancellationToken);
    }
  }

  private async Task ProcessLegacyInferenceAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var messageType = ResolveLegacyMessageType(request.LlmMessageType, requestJson);
    switch (messageType)
    {
      case LlmMessageType.Chat:
        await ProcessChatAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Generate:
        await ProcessGenerateAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Create:
        await ProcessCreateAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Show:
        await ProcessShowAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Copy:
        await ProcessCopyAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Delete:
        await ProcessDeleteAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Pull:
        await ProcessPullAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Push:
        await ProcessPushAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Embed:
        await ProcessEmbedAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Embeddings:
        await ProcessEmbeddingsAsync(request, requestJson, responseStream, cancellationToken);
        break;
      case LlmMessageType.Tags:
        await ProcessTagsAsync(request, responseStream, cancellationToken);
        break;
      case LlmMessageType.Ps:
        await ProcessPsAsync(request, responseStream, cancellationToken);
        break;
      case LlmMessageType.Version:
        await ProcessVersionAsync(request, responseStream, cancellationToken);
        break;
      case LlmMessageType.BlobHead:
      case LlmMessageType.BlobCreate:
        await WriteErrorAsync(request, messageType, "Blob operations require endpoint metadata.", responseStream, cancellationToken);
        break;
      default:
        await WriteErrorAsync(request, messageType, "Unsupported endpoint and unknown llm_message_type.", responseStream, cancellationToken);
        break;
    }
  }

  private async Task ProcessChatAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var chatRequest = JsonSerializer.Deserialize<OllamaChatRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse chat request.");

    chatRequest.Model = ResolveModel(chatRequest.Model);
    EnsureKeepAlive(chatRequest);

    if (chatRequest.Stream == true)
    {
      ulong chunkId = 1;
      await foreach (var chunk in _ollama.StreamChatJsonAsync(chatRequest, cancellationToken))
      {
        await WriteChunkAsync(request, LlmMessageType.Chat, chunk, chunkId++, responseStream, cancellationToken);
      }
      return;
    }

    var result = await _ollama.ChatAsync(chatRequest, cancellationToken)
      ?? throw new InvalidOperationException("Ollama returned no response.");
    await WriteChunkAsync(
      request,
      LlmMessageType.Chat,
      JsonSerializer.Serialize(result, _jsonOptions),
      1,
      responseStream,
      cancellationToken);
  }

  private async Task ProcessGenerateAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var generateRequest = JsonSerializer.Deserialize<OllamaGenerateRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse generate request.");

    generateRequest.Model = ResolveModel(generateRequest.Model);
    EnsureKeepAlive(generateRequest);

    if (generateRequest.Stream == true)
    {
      ulong chunkId = 1;
      await foreach (var chunk in _ollama.StreamGenerateJsonAsync(generateRequest, cancellationToken))
      {
        await WriteChunkAsync(request, LlmMessageType.Generate, chunk, chunkId++, responseStream, cancellationToken);
      }
      return;
    }

    var result = await _ollama.GenerateAsync(generateRequest, cancellationToken)
      ?? throw new InvalidOperationException("Ollama returned no response.");
    await WriteChunkAsync(
      request,
      LlmMessageType.Generate,
      JsonSerializer.Serialize(result, _jsonOptions),
      1,
      responseStream,
      cancellationToken);
  }

  private async Task ProcessCreateAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var createRequest = JsonSerializer.Deserialize<OllamaCreateModelRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse create request.");

    createRequest.Model = ResolveModel(createRequest.Model);

    if (createRequest.Stream == true)
    {
      ulong chunkId = 1;
      await foreach (var chunk in _ollama.StreamCreateModelJsonAsync(createRequest, cancellationToken))
      {
        await WriteChunkAsync(request, LlmMessageType.Create, chunk, chunkId++, responseStream, cancellationToken);
      }
      return;
    }

    var result = await _ollama.CreateModelAsync(createRequest, cancellationToken)
      ?? new OllamaStatusResponse { Status = "error" };
    await WriteChunkAsync(request, LlmMessageType.Create, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessShowAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var showRequest = JsonSerializer.Deserialize<OllamaShowRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse show request.");

    showRequest.Model = ResolveModel(showRequest.Model);
    var result = await _ollama.ShowAsync(showRequest, cancellationToken)
      ?? throw new InvalidOperationException("Ollama returned no response.");
    await WriteChunkAsync(request, LlmMessageType.Show, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessCopyAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var copyRequest = JsonSerializer.Deserialize<OllamaCopyModelRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse copy request.");
    var result = await _ollama.CopyModelAsync(copyRequest, cancellationToken)
      ?? new OllamaStatusResponse { Status = "error" };
    await WriteChunkAsync(request, LlmMessageType.Copy, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessDeleteAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var deleteRequest = JsonSerializer.Deserialize<OllamaDeleteModelRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse delete request.");

    deleteRequest.Model = ResolveModel(deleteRequest.Model);
    var result = await _ollama.DeleteModelAsync(deleteRequest, cancellationToken)
      ?? new OllamaStatusResponse { Status = "error" };
    await WriteChunkAsync(request, LlmMessageType.Delete, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessPullAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var pullRequest = JsonSerializer.Deserialize<OllamaPullModelRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse pull request.");

    pullRequest.Model = ResolveModel(pullRequest.Model);

    if (pullRequest.Stream == true)
    {
      ulong chunkId = 1;
      await foreach (var chunk in _ollama.StreamPullModelJsonAsync(pullRequest, cancellationToken))
      {
        await WriteChunkAsync(request, LlmMessageType.Pull, chunk, chunkId++, responseStream, cancellationToken);
      }
      return;
    }

    var result = await _ollama.PullModelAsync(pullRequest, cancellationToken)
      ?? new OllamaStatusResponse { Status = "error" };
    await WriteChunkAsync(request, LlmMessageType.Pull, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessPushAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var pushRequest = JsonSerializer.Deserialize<OllamaPushModelRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse push request.");

    pushRequest.Model = ResolveModel(pushRequest.Model);

    if (pushRequest.Stream == true)
    {
      ulong chunkId = 1;
      await foreach (var chunk in _ollama.StreamPushModelJsonAsync(pushRequest, cancellationToken))
      {
        await WriteChunkAsync(request, LlmMessageType.Push, chunk, chunkId++, responseStream, cancellationToken);
      }
      return;
    }

    var result = await _ollama.PushModelAsync(pushRequest, cancellationToken)
      ?? new OllamaStatusResponse { Status = "error" };
    await WriteChunkAsync(request, LlmMessageType.Push, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessEmbedAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var embedRequest = JsonSerializer.Deserialize<OllamaEmbedRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse embed request.");

    embedRequest.Model = ResolveModel(embedRequest.Model);
    EnsureKeepAlive(embedRequest);
    var result = await _ollama.EmbedAsync(embedRequest, cancellationToken)
      ?? throw new InvalidOperationException("Ollama returned no response.");
    await WriteChunkAsync(request, LlmMessageType.Embed, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessEmbeddingsAsync(
      ClientMessage request,
      string requestJson,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var embeddingsRequest = JsonSerializer.Deserialize<OllamaEmbeddingsRequest>(requestJson, _jsonOptions)
      ?? throw new InvalidOperationException("Failed to parse embeddings request.");

    embeddingsRequest.Model = ResolveModel(embeddingsRequest.Model);
    EnsureKeepAlive(embeddingsRequest);
    var result = await _ollama.EmbeddingsAsync(embeddingsRequest, cancellationToken)
      ?? throw new InvalidOperationException("Ollama returned no response.");
    await WriteChunkAsync(request, LlmMessageType.Embeddings, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessTagsAsync(
      ClientMessage request,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var result = await _ollama.TagsAsync(cancellationToken) ?? new OllamaTagsResponse();
    await WriteChunkAsync(request, LlmMessageType.Tags, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessPsAsync(
      ClientMessage request,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var result = await _ollama.PsAsync(cancellationToken) ?? new OllamaPsResponse();
    await WriteChunkAsync(request, LlmMessageType.Ps, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessVersionAsync(
      ClientMessage request,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var result = await _ollama.VersionAsync(cancellationToken) ?? new OllamaVersionResponse { Version = "unknown" };
    await WriteChunkAsync(request, LlmMessageType.Version, JsonSerializer.Serialize(result, _jsonOptions), 1, responseStream, cancellationToken);
  }

  private async Task ProcessBlobHeadAsync(
      ClientMessage request,
      string digest,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var statusCode = await _ollama.HeadBlobAsync(digest, cancellationToken);
    var responseJson = JsonSerializer.Serialize(new BlobStatusResponse(statusCode), _jsonOptions);
    await WriteChunkAsync(request, LlmMessageType.BlobHead, responseJson, 1, responseStream, cancellationToken);
  }

  private async Task ProcessBlobPostAsync(
      ClientMessage request,
      string digest,
      byte[] payload,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var statusCode = await _ollama.CreateBlobAsync(digest, payload, cancellationToken);
    var responseJson = JsonSerializer.Serialize(new BlobStatusResponse(statusCode), _jsonOptions);
    await WriteChunkAsync(request, LlmMessageType.BlobCreate, responseJson, 1, responseStream, cancellationToken);
  }

  private async Task WriteChunkAsync(
      ClientMessage request,
      LlmMessageType llmMessageType,
      string responseJson,
      ulong chunkId,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    var compressionFormat = CompressedPayloadCodec.NormalizeFormat(_options.CompressionFormat);
    var uncompressed = Encoding.UTF8.GetBytes(responseJson);
    var compressed = CompressedPayloadCodec.Compress(uncompressed, compressionFormat);

    await responseStream.WriteAsync(new ServerMessage
    {
      Header = new ServerHeader
      {
        SessionId = request.Header?.SessionId ?? string.Empty,
        MessageType = request.Header?.MessageType ?? SurfMessageType.Inference,
        MessageResult = SurfMessageResult.Success,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
      },
      Endpoint = request.Endpoint,
      LlmMessageType = llmMessageType,
      LlmMessage = new LlmMessage
      {
        Id = chunkId,
        CompressionFormat = compressionFormat,
        CompressedData = Google.Protobuf.ByteString.CopyFrom(compressed),
        UncompressedSize = (ulong)uncompressed.Length
      }
    }, cancellationToken);
  }

  private async Task WriteErrorAsync(
      ClientMessage request,
      LlmMessageType llmMessageType,
      string reason,
      IServerStreamWriter<ServerMessage> responseStream,
      CancellationToken cancellationToken)
  {
    await responseStream.WriteAsync(new ServerMessage
    {
      Header = new ServerHeader
      {
        SessionId = request.Header?.SessionId ?? string.Empty,
        MessageType = request.Header?.MessageType ?? SurfMessageType.Inference,
        MessageResult = SurfMessageResult.Error,
        Reason = reason,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
      },
      Endpoint = request.Endpoint,
      LlmMessageType = llmMessageType
    }, cancellationToken);
  }

  private string ResolveModel(string model)
  {
    return string.IsNullOrWhiteSpace(model) || model == "default" ? _options.DefaultModel : model;
  }

  private void EnsureKeepAlive(OllamaChatRequest request)
  {
    request.KeepAlive ??= _keepAlive;
  }

  private void EnsureKeepAlive(OllamaGenerateRequest request)
  {
    request.KeepAlive ??= _keepAlive;
  }

  private void EnsureKeepAlive(OllamaEmbedRequest request)
  {
    request.KeepAlive ??= _keepAlive;
  }

  private void EnsureKeepAlive(OllamaEmbeddingsRequest request)
  {
    request.KeepAlive ??= _keepAlive;
  }

  private static JsonElement ParseKeepAlive(string keepAliveValue)
  {
    var trimmed = string.IsNullOrWhiteSpace(keepAliveValue) ? "-1" : keepAliveValue.Trim();
    if (long.TryParse(trimmed, out var wholeSeconds))
    {
      return JsonSerializer.SerializeToElement(wholeSeconds);
    }

    if (double.TryParse(trimmed, out var fractionalSeconds))
    {
      return JsonSerializer.SerializeToElement(fractionalSeconds);
    }

    return JsonSerializer.SerializeToElement(trimmed);
  }

  private static string NormalizeEndpoint(string? endpoint)
  {
    return string.IsNullOrWhiteSpace(endpoint)
      ? string.Empty
      : endpoint.Trim().TrimStart('/');
  }

  private static string GetJsonPayload(byte[] payload)
  {
    if (payload.Length == 0)
    {
      return "{}";
    }

    return Encoding.UTF8.GetString(payload);
  }

  private static LlmMessageType ResolveLegacyMessageType(LlmMessageType llmMessageType, string requestJson)
  {
    if (llmMessageType != LlmMessageType.Unknown)
    {
      return llmMessageType;
    }

    using var document = JsonDocument.Parse(requestJson);
    if (document.RootElement.ValueKind != JsonValueKind.Object)
    {
      return LlmMessageType.Unknown;
    }

    if (document.RootElement.TryGetProperty("messages", out _))
    {
      return LlmMessageType.Chat;
    }

    if (document.RootElement.TryGetProperty("prompt", out _) ||
        document.RootElement.TryGetProperty("suffix", out _) ||
        document.RootElement.TryGetProperty("images", out _))
    {
      return LlmMessageType.Generate;
    }

    return LlmMessageType.Unknown;
  }

  private sealed record BlobStatusResponse(int StatusCode);
}
