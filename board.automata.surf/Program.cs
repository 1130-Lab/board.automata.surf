using board.automata.surf.services;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<SessionStoreOptions>(builder.Configuration.GetSection("SessionStore"));
builder.Services.Configure<SessionTokenOptions>(builder.Configuration.GetSection("SessionTokens"));

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

app.UseWebSockets(new WebSocketOptions
{
  KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.MapControllers();

app.Run();
