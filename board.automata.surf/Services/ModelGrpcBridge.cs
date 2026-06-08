using System.Runtime.CompilerServices;
using board.automata.surf.api;
using board.automata.surf.proto;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace board.automata.surf.services;

public sealed class ModelGrpcBridge
{
  private readonly ModelBridgeOptions _options;
  private readonly ILogger<ModelGrpcBridge> _logger;

  public ModelGrpcBridge(IOptions<ModelBridgeOptions> options, ILogger<ModelGrpcBridge> logger)
  {
    _options = options.Value;
    _logger = logger;
  }

  public async IAsyncEnumerable<string> StreamInferenceAsync(
      string sessionId,
      string endpoint,
      LlmMessageType llmMessageType,
      byte[] requestPayload,
      [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    var compressionFormat = CompressedPayloadCodec.NormalizeFormat(_options.CompressionFormat);
    var compressedPayload = CompressedPayloadCodec.Compress(requestPayload, compressionFormat);

    using var channel = GrpcChannel.ForAddress(_options.GrpcEndpoint);
    var client = new ModelSurfness.ModelSurfnessClient(channel);

    using var call = client.ExchangeMessages(cancellationToken: cancellationToken);

    await call.RequestStream.WriteAsync(new ClientMessage
    {
      Header = new ClientHeader
      {
        SessionId = sessionId,
        MessageType = SurfMessageType.Inference,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
      },
      LlmMessageType = llmMessageType,
      Endpoint = endpoint,
      LlmMessage = new LlmMessage
      {
        Id = 1,
        CompressedData = Google.Protobuf.ByteString.CopyFrom(compressedPayload),
        CompressionFormat = compressionFormat,
        UncompressedSize = (ulong)requestPayload.Length,
        TokenCount = 0
      }
    }, cancellationToken);

    await call.RequestStream.CompleteAsync();

    while (await call.ResponseStream.MoveNext(cancellationToken))
    {
      var response = call.ResponseStream.Current;
      if (response.Header is not null && response.Header.MessageResult == SurfMessageResult.Error)
      {
        var reason = string.IsNullOrWhiteSpace(response.Header.Reason)
          ? "Board returned an error."
          : response.Header.Reason;
        throw new InvalidOperationException(reason);
      }

      if (response.LlmMessage is null || response.LlmMessage.CompressedData.IsEmpty)
      {
        _logger.LogDebug("Received empty board response for session {SessionId}", sessionId);
        continue;
      }

      var payload = response.LlmMessage.CompressedData.ToByteArray();
      yield return CompressedPayloadCodec.DecompressToUtf8(payload, response.LlmMessage.CompressionFormat);
    }
  }
}
