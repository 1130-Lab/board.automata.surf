using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using board.automata.surf.api.models;

namespace board.automata.surf.boards.ollama;

public sealed class OllamaClient : IDisposable
{
  private readonly HttpClient _http;
  private readonly string _baseUrl;

  public OllamaClient(string baseUrl = "http://localhost:11434")
  {
    _baseUrl = baseUrl.TrimEnd('/');
    _http = new HttpClient
    {
      BaseAddress = new Uri(_baseUrl),
      Timeout = TimeSpan.FromMinutes(30)
    };
  }

  public async Task<bool> IsReachableAsync(CancellationToken ct = default)
  {
    try
    {
      var resp = await _http.GetAsync("/api/version", ct);
      return resp.IsSuccessStatusCode;
    }
    catch
    {
      return false;
    }
  }

  public async Task<OllamaGenerateResponse?> GenerateAsync(OllamaGenerateRequest request, CancellationToken ct = default)
  {
    var resp = await _http.PostAsJsonAsync("/api/generate", request, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct);
  }

  public IAsyncEnumerable<string> StreamGenerateJsonAsync(OllamaGenerateRequest request, CancellationToken ct = default)
  {
    return StreamJsonLinesAsync("/api/generate", request, ct);
  }

  public async Task<OllamaChatResponse?> ChatAsync(OllamaChatRequest request, CancellationToken ct = default)
  {
    var resp = await _http.PostAsJsonAsync("/api/chat", request, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);
  }

  public IAsyncEnumerable<string> StreamChatJsonAsync(OllamaChatRequest request, CancellationToken ct = default)
  {
    return StreamJsonLinesAsync("/api/chat", request, ct);
  }

  public async Task<OllamaShowResponse?> ShowAsync(OllamaShowRequest request, CancellationToken ct = default)
  {
    var resp = await _http.PostAsJsonAsync("/api/show", request, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaShowResponse>(cancellationToken: ct);
  }

  public async Task<OllamaStatusResponse?> CreateModelAsync(OllamaCreateModelRequest request, CancellationToken ct = default)
  {
    var resp = await _http.PostAsJsonAsync("/api/create", request, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaStatusResponse>(cancellationToken: ct);
  }

  public IAsyncEnumerable<string> StreamCreateModelJsonAsync(OllamaCreateModelRequest request, CancellationToken ct = default)
  {
    return StreamJsonLinesAsync("/api/create", request, ct);
  }

  public async Task<OllamaStatusResponse?> CopyModelAsync(OllamaCopyModelRequest request, CancellationToken ct = default)
  {
    var resp = await _http.PostAsJsonAsync("/api/copy", request, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaStatusResponse>(cancellationToken: ct);
  }

  public async Task<OllamaStatusResponse?> DeleteModelAsync(OllamaDeleteModelRequest request, CancellationToken ct = default)
  {
    var msg = new HttpRequestMessage(HttpMethod.Delete, "/api/delete")
    {
      Content = JsonContent.Create(request)
    };
    var resp = await _http.SendAsync(msg, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaStatusResponse>(cancellationToken: ct);
  }

  public async Task<OllamaStatusResponse?> PullModelAsync(OllamaPullModelRequest request, CancellationToken ct = default)
  {
    var resp = await _http.PostAsJsonAsync("/api/pull", request, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaStatusResponse>(cancellationToken: ct);
  }

  public IAsyncEnumerable<string> StreamPullModelJsonAsync(OllamaPullModelRequest request, CancellationToken ct = default)
  {
    return StreamJsonLinesAsync("/api/pull", request, ct);
  }

  public async Task<OllamaStatusResponse?> PushModelAsync(OllamaPushModelRequest request, CancellationToken ct = default)
  {
    var resp = await _http.PostAsJsonAsync("/api/push", request, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaStatusResponse>(cancellationToken: ct);
  }

  public IAsyncEnumerable<string> StreamPushModelJsonAsync(OllamaPushModelRequest request, CancellationToken ct = default)
  {
    return StreamJsonLinesAsync("/api/push", request, ct);
  }

  public async Task<OllamaEmbedResponse?> EmbedAsync(OllamaEmbedRequest request, CancellationToken ct = default)
  {
    var resp = await _http.PostAsJsonAsync("/api/embed", request, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken: ct);
  }

  public async Task<OllamaEmbeddingsResponse?> EmbeddingsAsync(OllamaEmbeddingsRequest request, CancellationToken ct = default)
  {
    var resp = await _http.PostAsJsonAsync("/api/embeddings", request, ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaEmbeddingsResponse>(cancellationToken: ct);
  }

  public async Task<OllamaTagsResponse?> TagsAsync(CancellationToken ct = default)
  {
    var resp = await _http.GetAsync("/api/tags", ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken: ct);
  }

  public async Task<OllamaPsResponse?> PsAsync(CancellationToken ct = default)
  {
    var resp = await _http.GetAsync("/api/ps", ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaPsResponse>(cancellationToken: ct);
  }

  public async Task<OllamaVersionResponse?> VersionAsync(CancellationToken ct = default)
  {
    var resp = await _http.GetAsync("/api/version", ct);
    resp.EnsureSuccessStatusCode();
    return await resp.Content.ReadFromJsonAsync<OllamaVersionResponse>(cancellationToken: ct);
  }

  public async Task<int> HeadBlobAsync(string digest, CancellationToken ct = default)
  {
    using var message = new HttpRequestMessage(HttpMethod.Head, $"/api/blobs/{Uri.EscapeDataString(digest)}");
    using var response = await _http.SendAsync(message, ct);
    return (int)response.StatusCode;
  }

  public async Task<int> CreateBlobAsync(string digest, byte[] data, CancellationToken ct = default)
  {
    using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/blobs/{Uri.EscapeDataString(digest)}")
    {
      Content = new ByteArrayContent(data)
    };
    message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
    using var response = await _http.SendAsync(message, ct);
    return (int)response.StatusCode;
  }

  private async IAsyncEnumerable<string> StreamJsonLinesAsync<TRequest>(
      string path,
      TRequest request,
      [EnumeratorCancellation] CancellationToken ct)
  {
    using var message = new HttpRequestMessage(HttpMethod.Post, path)
    {
      Content = JsonContent.Create(request)
    };

    using var response = await _http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
    response.EnsureSuccessStatusCode();

    using var stream = await response.Content.ReadAsStreamAsync(ct);
    using var reader = new StreamReader(stream);

    while (!ct.IsCancellationRequested)
    {
      var line = await reader.ReadLineAsync(ct);
      if (line is null)
      {
        break;
      }

      if (string.IsNullOrWhiteSpace(line))
      {
        continue;
      }

      yield return line;
    }
  }

  public void Dispose() => _http.Dispose();
}
