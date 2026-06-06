using System.Net.Http.Json;
using System.Text.Json;
using board.automata.surf.api.models;
using board.automata.surf.boards.ollama;

var ollamaHost = Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://localhost:11434";
var boardHost = Environment.GetEnvironmentVariable("BOARD_HOST") ?? "http://localhost:5209";
var listenPrefixes = (Environment.GetEnvironmentVariable("LISTEN_PREFIXES") ?? "http://0.0.0.0:11435").Split(';');
var defaultModel = Environment.GetEnvironmentVariable("GEMMA4_MODEL") ?? "gemma4:e4b";

await EnsureOllamaRunningAsync(ollamaHost);

using var ollama = new OllamaClient(ollamaHost);
var board = new OllamaBoard(ollama, boardHost, defaultModel);

if (!await board.EnsureModelAvailableAsync(ct: CancellationToken.None))
{
  Console.Error.WriteLine("Failed to pull model. Exiting.");
  return 1;
}

Console.WriteLine($"Ollama board ready. Proxying {ollamaHost} -> board.automata.surf at {boardHost}");
Console.WriteLine($"Default model: {defaultModel}");

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls(listenPrefixes);

builder.Services.AddSingleton(ollama);
builder.Services.AddSingleton(board);

var app = builder.Build();

app.MapPost("/api/generate", async (OllamaGenerateRequest req, OllamaClient client) =>
{
  req.Model = ResolveModel(req.Model, defaultModel);
  var result = await client.GenerateAsync(req);
  return Results.Json(result);
});

app.MapPost("/api/chat", async (OllamaChatRequest req, OllamaClient client) =>
{
  req.Model = ResolveModel(req.Model, defaultModel);
  var result = await client.ChatAsync(req);
  return Results.Json(result);
});

app.MapPost("/api/create", async (OllamaCreateModelRequest req, OllamaClient client) =>
{
  var result = await client.CreateModelAsync(req);
  return Results.Json(result);
});

app.MapPost("/api/show", async (OllamaShowRequest req, OllamaClient client) =>
{
  req.Model = ResolveModel(req.Model, defaultModel);
  var result = await client.ShowAsync(req);
  return Results.Json(result);
});

app.MapPost("/api/copy", async (OllamaCopyModelRequest req, OllamaClient client) =>
{
  var result = await client.CopyModelAsync(req);
  return Results.Json(result);
});

app.MapDelete("/api/delete", async (HttpContext ctx, OllamaClient client) =>
{
  var req = await ctx.Request.ReadFromJsonAsync<OllamaDeleteModelRequest>();
  if (req is null) return Results.BadRequest();
  var result = await client.DeleteModelAsync(req);
  return Results.Json(result);
});

app.MapPost("/api/pull", async (OllamaPullModelRequest req, OllamaClient client) =>
{
  var result = await client.PullModelAsync(req);
  return Results.Json(result);
});

app.MapPost("/api/push", async (OllamaPushModelRequest req, OllamaClient client) =>
{
  var result = await client.PushModelAsync(req);
  return Results.Json(result);
});

app.MapPost("/api/embed", async (OllamaEmbedRequest req, OllamaClient client) =>
{
  req.Model = ResolveModel(req.Model, defaultModel);
  var result = await client.EmbedAsync(req);
  return Results.Json(result);
});

app.MapPost("/api/embeddings", async (OllamaEmbeddingsRequest req, OllamaClient client) =>
{
  var result = await client.EmbeddingsAsync(req);
  return Results.Json(result);
});

app.MapGet("/api/tags", async (OllamaClient client) =>
{
  var result = await client.TagsAsync();
  return Results.Json(result);
});

app.MapGet("/api/ps", async (OllamaClient client) =>
{
  var result = await client.PsAsync();
  return Results.Json(result);
});

app.MapGet("/api/version", async (OllamaClient client) =>
{
  var result = await client.VersionAsync();
  return Results.Json(result);
});

app.MapGet("/", () => Results.Json(new
{
  board = new OllamaBoard(ollama, boardHost, defaultModel),
  status = "ready",
  model = defaultModel,
  ollama = ollamaHost,
  upstream = boardHost
}));

_ = ForwardToBoardAsync(board, ollama, boardHost);

await app.RunAsync();
return 0;

static string ResolveModel(string? model, string defaultModel)
{
  return string.IsNullOrWhiteSpace(model) || model == "default" ? defaultModel : model;
}

static async Task EnsureOllamaRunningAsync(string ollamaHost)
{
  using var probe = new HttpClient { BaseAddress = new Uri(ollamaHost) };
  for (var i = 0; i < 60; i++)
  {
    try
    {
      var resp = await probe.GetAsync("/api/version");
      if (resp.IsSuccessStatusCode)
      {
        var ver = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Console.WriteLine($"Connected to Ollama {ver.GetProperty("version").GetString()}");
        return;
      }
    }
    catch
    {
      // Not ready yet
    }
    Console.WriteLine("Waiting for Ollama...");
    await Task.Delay(2000);
  }
  throw new InvalidOperationException("Ollama did not become reachable.");
}

static async Task ForwardToBoardAsync(OllamaBoard board, OllamaClient ollama, string boardHost)
{
  using var upstream = new HttpClient { BaseAddress = new Uri(boardHost) };
  while (true)
  {
    try
    {
      var tags = await ollama.TagsAsync();
      if (tags?.Models?.Count > 0)
      {
        await upstream.PostAsJsonAsync("/api/generate", new
        {
          model = board.BoardName,
          prompt = "heartbeat"
        });
      }
    }
    catch
    {
      // Upstream might not be ready
    }
    await Task.Delay(TimeSpan.FromSeconds(30));
  }
}
