using System.Buffers;
using System.Net.WebSockets;
using board.automata.surf.proto;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;

namespace board.automata.surf.services;

public sealed class WebSocketRelayService
{
  private readonly SessionStore _sessionStore;
  private readonly SessionTokenService _tokenService;
  private readonly ILogger<WebSocketRelayService> _logger;

  public WebSocketRelayService(
      SessionStore sessionStore,
      SessionTokenService tokenService,
      ILogger<WebSocketRelayService> logger)
  {
    _sessionStore = sessionStore;
    _tokenService = tokenService;
    _logger = logger;
  }

  public async Task HandleAsync(HttpContext context)
  {
    if (!context.WebSockets.IsWebSocketRequest)
    {
      context.Response.StatusCode = StatusCodes.Status400BadRequest;
      return;
    }

    var sessionId = context.Request.Query["sid"].ToString();
    if (string.IsNullOrWhiteSpace(sessionId))
    {
      context.Response.StatusCode = StatusCodes.Status400BadRequest;
      await context.Response.WriteAsync("Missing sid query parameter.");
      return;
    }

    var bearerToken = ExtractBearerToken(context.Request.Headers.Authorization.ToString());
    if (string.IsNullOrWhiteSpace(bearerToken) ||
        !_tokenService.TryValidate(bearerToken, out var principal) ||
        principal is null ||
        !_tokenService.TryGetSessionId(principal, out var tokenSessionId) ||
        !string.Equals(tokenSessionId, sessionId, StringComparison.Ordinal))
    {
      context.Response.StatusCode = StatusCodes.Status401Unauthorized;
      return;
    }

    if (!_sessionStore.TryGetActiveSession(sessionId, out var session))
    {
      context.Response.StatusCode = StatusCodes.Status404NotFound;
      return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    _sessionStore.Touch(sessionId);

    try
    {
      await RelayAsync(webSocket, sessionId, session.ModelEndpoint, context.RequestAborted);
    }
    finally
    {
      _sessionStore.Close(sessionId, "websocket closed");
    }
  }

  private async Task RelayAsync(
      WebSocket webSocket,
      string sessionId,
      string modelEndpoint,
      CancellationToken cancellationToken)
  {
    using var channel = GrpcChannel.ForAddress(modelEndpoint);
    var modelClient = new ModelSurfness.ModelSurfnessClient(channel);
    /* Route model -> end user via websocket */
  }

  private async Task WebSocketToGrpcAsync(
      WebSocket webSocket,
      IClientStreamWriter<ClientMessage> requestStream,
      string sessionId,
      CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
    {
      var frame = await ReceiveBinaryFrameAsync(webSocket, cancellationToken);
      if (frame.IsClose)
      {
        break;
      }

      if (frame.Payload is null || frame.Payload.Length == 0)
      {
        continue;
      }

      ClientMessage message;
      try
      {
        message = ClientMessage.Parser.ParseFrom(frame.Payload);
      }
      catch (InvalidProtocolBufferException ex)
      {
        _logger.LogWarning(ex, "Invalid websocket payload for session {SessionId}", sessionId);
        continue;
      }

      await requestStream.WriteAsync(message, cancellationToken);
      _sessionStore.Touch(sessionId);
    }
  }

  private async Task GrpcToWebSocketAsync(
      IAsyncStreamReader<ServerMessage> responseStream,
      WebSocket webSocket,
      string sessionId,
      CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested &&
           webSocket.State == WebSocketState.Open &&
           await responseStream.MoveNext(cancellationToken))
    {
      var payload = responseStream.Current.ToByteArray();
      await webSocket.SendAsync(
          new ArraySegment<byte>(payload),
          WebSocketMessageType.Binary,
          endOfMessage: true,
          cancellationToken);

      _sessionStore.Touch(sessionId);
    }
  }

  private static async Task<(bool IsClose, byte[]? Payload)> ReceiveBinaryFrameAsync(
      WebSocket socket,
      CancellationToken cancellationToken)
  {
    var buffer = ArrayPool<byte>.Shared.Rent(4 * 1024);
    try
    {
      using var stream = new MemoryStream();
      while (true)
      {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

        if (result.MessageType == WebSocketMessageType.Close)
        {
          return (true, null);
        }

        if (result.MessageType != WebSocketMessageType.Binary)
        {
          return (true, null);
        }

        if (result.Count > 0)
        {
          stream.Write(buffer, 0, result.Count);
        }

        if (result.EndOfMessage)
        {
          break;
        }
      }

      return (false, stream.ToArray());
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer);
    }
  }

  private static string? ExtractBearerToken(string authorizationHeader)
  {
    const string prefix = "Bearer ";
    if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return authorizationHeader[prefix.Length..].Trim();
  }
}
