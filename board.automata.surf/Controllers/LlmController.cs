using System.Text;
using System.Text.Json;
using board.automata.surf.api.models;
using board.automata.surf.proto;
using board.automata.surf.services;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace board.automata.surf.controllers;

[ApiController]
[Route("api")]
public sealed class LlmController : ControllerBase
{
  private readonly ILogger<LlmController> _logger;
  private readonly ModelGrpcBridge _modelBridge;

  public LlmController(ILogger<LlmController> logger, ModelGrpcBridge modelBridge)
  {
    _logger = logger;
    _modelBridge = modelBridge;
  }

  [HttpPost("generate")]
  public async Task<IActionResult> Generate([FromBody] OllamaGenerateRequest request, CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "generate",
      llmMessageType: LlmMessageType.Generate,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: IsStreamingRequest(requestJson),
      cancellationToken: cancellationToken);
  }

  [HttpPost("chat")]
  public async Task<IActionResult> Chat(CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "chat",
      llmMessageType: LlmMessageType.Chat,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: IsStreamingRequest(requestJson),
      cancellationToken: cancellationToken);
  }

  [HttpPost("create")]
  public async Task<IActionResult> Create(CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "create",
      llmMessageType: LlmMessageType.Create,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: IsStreamingRequest(requestJson),
      cancellationToken: cancellationToken);
  }

  [HttpHead("blobs/{digest}")]
  public Task<IActionResult> HeadBlob([FromRoute] string digest, CancellationToken cancellationToken)
  {
    return RelayBlobStatusEndpointAsync(
      $"blobs/head/{digest}",
      LlmMessageType.BlobHead,
      Array.Empty<byte>(),
      cancellationToken);
  }

  [HttpPost("blobs/{digest}")]
  public async Task<IActionResult> CreateBlob([FromRoute] string digest, CancellationToken cancellationToken)
  {
    byte[] payload;
    using (var memory = new MemoryStream())
    {
      await Request.Body.CopyToAsync(memory, cancellationToken);
      payload = memory.ToArray();
    }

    return await RelayBlobStatusEndpointAsync(
      $"blobs/post/{digest}",
      LlmMessageType.BlobCreate,
      payload,
      cancellationToken);
  }

  [HttpGet("tags")]
  public Task<IActionResult> Tags(CancellationToken cancellationToken)
  {
    return RelayEndpointAsync(
      endpoint: "tags",
      llmMessageType: LlmMessageType.Tags,
      payload: Encoding.UTF8.GetBytes("{}"),
      streamRequested: false,
      cancellationToken: cancellationToken);
  }

  [HttpPost("show")]
  public async Task<IActionResult> Show(CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "show",
      llmMessageType: LlmMessageType.Show,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: false,
      cancellationToken: cancellationToken);
  }

  [HttpPost("copy")]
  public async Task<IActionResult> Copy(CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "copy",
      llmMessageType: LlmMessageType.Copy,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: false,
      cancellationToken: cancellationToken);
  }

  [HttpDelete("delete")]
  public async Task<IActionResult> Delete(CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "delete",
      llmMessageType: LlmMessageType.Delete,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: false,
      cancellationToken: cancellationToken);
  }

  [HttpPost("pull")]
  public async Task<IActionResult> Pull(CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "pull",
      llmMessageType: LlmMessageType.Pull,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: IsStreamingRequest(requestJson),
      cancellationToken: cancellationToken);
  }

  [HttpPost("push")]
  public async Task<IActionResult> Push(CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "push",
      llmMessageType: LlmMessageType.Push,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: IsStreamingRequest(requestJson),
      cancellationToken: cancellationToken);
  }

  [HttpPost("embed")]
  public async Task<IActionResult> Embed(CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "embed",
      llmMessageType: LlmMessageType.Embed,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: false,
      cancellationToken: cancellationToken);
  }

  [HttpGet("ps")]
  public Task<IActionResult> Ps(CancellationToken cancellationToken)
  {
    return RelayEndpointAsync(
      endpoint: "ps",
      llmMessageType: LlmMessageType.Ps,
      payload: Encoding.UTF8.GetBytes("{}"),
      streamRequested: false,
      cancellationToken: cancellationToken);
  }

  [HttpPost("embeddings")]
  public async Task<IActionResult> Embeddings(CancellationToken cancellationToken)
  {
    var requestJson = await ReadRequestJsonAsync(cancellationToken);
    if (requestJson is null)
    {
      return BadRequest(new { message = "Request body must be valid JSON." });
    }

    return await RelayEndpointAsync(
      endpoint: "embeddings",
      llmMessageType: LlmMessageType.Embeddings,
      payload: Encoding.UTF8.GetBytes(requestJson),
      streamRequested: false,
      cancellationToken: cancellationToken);
  }

  [HttpGet("version")]
  public Task<IActionResult> Version(CancellationToken cancellationToken)
  {
    return RelayEndpointAsync(
      endpoint: "version",
      llmMessageType: LlmMessageType.Version,
      payload: Encoding.UTF8.GetBytes("{}"),
      streamRequested: false,
      cancellationToken: cancellationToken);
  }

  private async Task<IActionResult> RelayBlobStatusEndpointAsync(
      string endpoint,
      LlmMessageType llmMessageType,
      byte[] payload,
      CancellationToken cancellationToken)
  {
    try
    {
      var resultJson = await RelayForSingleJsonChunkAsync(endpoint, llmMessageType, payload, cancellationToken);
      if (resultJson is null)
      {
        return StatusCode(StatusCodes.Status502BadGateway, new { message = "Board returned no response." });
      }

      BlobStatusResponse? status;
      try
      {
        status = JsonSerializer.Deserialize<BlobStatusResponse>(resultJson);
      }
      catch (JsonException ex)
      {
        _logger.LogWarning(ex, "Blob status payload from board was invalid JSON.");
        return StatusCode(StatusCodes.Status502BadGateway, new { message = "Board returned malformed response." });
      }

      if (status is null)
      {
        return StatusCode(StatusCodes.Status502BadGateway, new { message = "Board returned malformed response." });
      }

      return StatusCode(status.StatusCode);
    }
    catch (RpcException ex)
    {
      _logger.LogError(ex, "Board gRPC call failed for {Path}", Request.Path);
      return StatusCode(StatusCodes.Status502BadGateway, new { message = $"Board call failed: {ex.Status.Detail}" });
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Board rejected request for {Path}", Request.Path);
      return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
    }
    catch (InvalidDataException ex)
    {
      _logger.LogWarning(ex, "Board returned invalid compressed payload for {Path}", Request.Path);
      return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
    }
  }

  private async Task<IActionResult> RelayEndpointAsync(
      string endpoint,
      LlmMessageType llmMessageType,
      byte[] payload,
      bool streamRequested,
      CancellationToken cancellationToken)
  {
    try
    {
      var sessionId = Guid.CreateVersion7().ToString("N");
      var chunks = _modelBridge.StreamInferenceAsync(
        sessionId,
        endpoint,
        llmMessageType,
        payload,
        cancellationToken);

      if (streamRequested)
      {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson";

        await foreach (var chunk in chunks)
        {
          await Response.WriteAsync(chunk, cancellationToken);
          if (!chunk.EndsWith('\n'))
          {
            await Response.WriteAsync("\n", cancellationToken);
          }

          await Response.Body.FlushAsync(cancellationToken);
        }

        return new EmptyResult();
      }

      string? lastChunk = null;
      await foreach (var chunk in chunks)
      {
        lastChunk = chunk;
      }

      if (lastChunk is null)
      {
        return StatusCode(StatusCodes.Status502BadGateway, new { message = "Board returned no response." });
      }

      return Content(lastChunk, "application/json", Encoding.UTF8);
    }
    catch (RpcException ex)
    {
      _logger.LogError(ex, "Board gRPC call failed for {Path}", Request.Path);
      return StatusCode(StatusCodes.Status502BadGateway, new { message = $"Board call failed: {ex.Status.Detail}" });
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Board rejected request for {Path}", Request.Path);
      return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
    }
    catch (InvalidDataException ex)
    {
      _logger.LogWarning(ex, "Board returned invalid compressed payload for {Path}", Request.Path);
      return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
    }
  }

  private async Task<string?> RelayForSingleJsonChunkAsync(
      string endpoint,
      LlmMessageType llmMessageType,
      byte[] payload,
      CancellationToken cancellationToken)
  {
    var sessionId = Guid.CreateVersion7().ToString("N");
    var chunks = _modelBridge.StreamInferenceAsync(sessionId, endpoint, llmMessageType, payload, cancellationToken);

    string? lastChunk = null;
    await foreach (var chunk in chunks)
    {
      lastChunk = chunk;
    }

    return lastChunk;
  }

  private async Task<string?> ReadRequestJsonAsync(CancellationToken cancellationToken)
  {
    string requestJson;
    using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
    {
      requestJson = await reader.ReadToEndAsync(cancellationToken);
    }

    requestJson = string.IsNullOrWhiteSpace(requestJson) ? "{}" : requestJson;

    try
    {
      using var _ = JsonDocument.Parse(requestJson);
      return requestJson;
    }
    catch (JsonException ex)
    {
      _logger.LogWarning(ex, "Invalid JSON payload for {Path}", Request.Path);
      return null;
    }
  }

  private static bool IsStreamingRequest(string requestJson)
  {
    using var document = JsonDocument.Parse(requestJson);
    return document.RootElement.ValueKind == JsonValueKind.Object &&
           document.RootElement.TryGetProperty("stream", out var streamElement) &&
           streamElement.ValueKind == JsonValueKind.True;
  }

  private sealed record BlobStatusResponse(int StatusCode);
}
