using System.IO.Compression;
using System.Text;

namespace board.automata.surf.api;

public static class CompressedPayloadCodec
{
  public const string Gzip = "gzip";

  public static byte[] CompressUtf8(string payload, string? compressionFormat)
  {
    ArgumentNullException.ThrowIfNull(payload);
    return Compress(Encoding.UTF8.GetBytes(payload), compressionFormat);
  }

  public static byte[] Compress(byte[] payload, string? compressionFormat)
  {
    ArgumentNullException.ThrowIfNull(payload);
    var format = NormalizeFormat(compressionFormat);

    return format switch
    {
      Gzip => CompressWithGzip(payload),
      _ => throw new InvalidOperationException($"Unsupported compression format '{compressionFormat}'.")
    };
  }

  public static string DecompressToUtf8(byte[] payload, string? compressionFormat)
  {
    return Encoding.UTF8.GetString(Decompress(payload, compressionFormat));
  }

  public static byte[] Decompress(byte[] payload, string? compressionFormat)
  {
    ArgumentNullException.ThrowIfNull(payload);
    var format = NormalizeFormat(compressionFormat);

    return format switch
    {
      Gzip => DecompressGzip(payload),
      _ => throw new InvalidOperationException($"Unsupported compression format '{compressionFormat}'.")
    };
  }

  public static string NormalizeFormat(string? compressionFormat)
  {
    return string.IsNullOrWhiteSpace(compressionFormat)
      ? Gzip
      : compressionFormat.Trim().ToLowerInvariant();
  }

  private static byte[] CompressWithGzip(byte[] payload)
  {
    using var output = new MemoryStream();
    using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
    {
      gzip.Write(payload, 0, payload.Length);
    }

    return output.ToArray();
  }

  private static byte[] DecompressGzip(byte[] payload)
  {
    using var input = new MemoryStream(payload);
    using var gzip = new GZipStream(input, CompressionMode.Decompress);
    using var output = new MemoryStream();
    gzip.CopyTo(output);
    return output.ToArray();
  }
}
