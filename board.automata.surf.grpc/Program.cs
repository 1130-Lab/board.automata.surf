using board.automata.surf.grpc.services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

var grpcPort = builder.Configuration.GetValue<int?>("Grpc:Port") ?? 50051;
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(grpcPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();
builder.Services.AddSingleton<PreparedSessionRegistry>();

var app = builder.Build();

app.MapGrpcService<ModelSurfnessImpl>();
app.MapGet("/", () => "Model gRPC server is running.");

app.Run();
