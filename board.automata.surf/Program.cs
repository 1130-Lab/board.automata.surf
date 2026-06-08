using board.automata.surf.services;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
var webSocketPath = builder.Configuration.GetValue<string>("ModelBridge:WebSocketPath") ?? "/ws";
const string localCorsPolicy = "LocalTesting";

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
  options.AddPolicy(localCorsPolicy, policy =>
  {
    policy
      .SetIsOriginAllowed(static origin => IsLocalOrigin(origin))
      .AllowAnyHeader()
      .AllowAnyMethod();
  });
});

builder.Services.Configure<ModelBridgeOptions>(builder.Configuration.GetSection("ModelBridge"));
builder.Services.Configure<SessionStoreOptions>(builder.Configuration.GetSection("SessionStore"));
builder.Services.Configure<SessionTokenOptions>(builder.Configuration.GetSection("SessionTokens"));

builder.Services.AddSingleton<ModelGrpcBridge>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<SessionTokenService>();
builder.Services.AddSingleton<WebSocketRelayService>();
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

/*
 * Idea: use CORS to filter requests. Add Beach by default, and add/remove peers from CORS list as necessary.
 */

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseCors(localCorsPolicy);

app.UseWebSockets(new WebSocketOptions
{
  KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.Map(webSocketPath, static async context =>
{
  var relay = context.RequestServices.GetRequiredService<WebSocketRelayService>();
  await relay.HandleAsync(context);
});

app.Map("/ws/secure", static async context =>
{
  var relay = context.RequestServices.GetRequiredService<WebSocketRelayService>();
  await relay.HandleAsync(context);
});

app.MapControllers();

app.Run();

static bool IsLocalOrigin(string origin)
{
  if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
  {
    return false;
  }

  if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
      !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
  {
    return false;
  }

  return uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
}
