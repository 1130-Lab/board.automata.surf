using System.Text.Json;
using board.automata.surf.api.models;
using board.automata.surf.boards.ollama;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var ollamaHost = Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://localhost:11434";
var defaultModel = Environment.GetEnvironmentVariable("GEMMA4_MODEL") ?? "gemma4:e4b";
var compressionFormat = Environment.GetEnvironmentVariable("BOARD_COMPRESSION_FORMAT") ?? "gzip";
var modelKeepAlive = Environment.GetEnvironmentVariable("BOARD_MODEL_KEEP_ALIVE") ?? "-1";
var grpcPort = int.TryParse(Environment.GetEnvironmentVariable("BOARD_GRPC_PORT"), out var parsedPort)
  ? parsedPort
  : 50061;

await EnsureOllamaRunningAsync(ollamaHost);

using (var bootstrapClient = new OllamaClient(ollamaHost))
{
  if (!await EnsureModelAvailableAsync(bootstrapClient, defaultModel, CancellationToken.None))
  {
    Console.Error.WriteLine($"Failed to pull model '{defaultModel}'. Exiting.");
    return 1;
  }
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
  options.ListenAnyIP(grpcPort, listenOptions =>
  {
    listenOptions.Protocols = HttpProtocols.Http2;
  });
});

builder.Services.AddGrpc();
builder.Services.AddSingleton(_ => new OllamaClient(ollamaHost));
builder.Services.AddSingleton(new OllamaBoardRuntimeOptions
{
  DefaultModel = defaultModel,
  CompressionFormat = compressionFormat,
  ModelKeepAlive = modelKeepAlive
});

var app = builder.Build();
app.MapGrpcService<OllamaBoardGrpcService>();
app.MapGet("/", () => Results.Json(new
{
  status = "ready",
  board = "ollama.board",
  ollama = ollamaHost,
  grpc_port = grpcPort,
  default_model = defaultModel,
  compression = compressionFormat,
  model_keep_alive = modelKeepAlive
}));

await app.RunAsync();
return 0;

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
    catch (HttpRequestException)
    {
      // Waiting for Ollama startup.
    }

    Console.WriteLine("Waiting for Ollama...");
    await Task.Delay(2000);
  }

  throw new InvalidOperationException("Ollama did not become reachable.");
}

static async Task<bool> EnsureModelAvailableAsync(OllamaClient ollamaClient, string model, CancellationToken ct)
{
  var tags = await ollamaClient.TagsAsync(ct);
  if (tags?.Models?.Any(m => m.Name == model || m.Model == model) == true)
  {
    return true;
  }

  Console.WriteLine($"Pulling {model}...");
  var result = await ollamaClient.PullModelAsync(new OllamaPullModelRequest { Model = model }, ct);
  return result?.Status == "success";
}
