using board.automata.surf.models;
using board.automata.surf.services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace board.automata.surf.controllers;

[ApiController]
[Route("api/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly SessionStore _sessionStore;
    private readonly SessionTokenService _tokenService;
    private readonly ModelBridgeOptions _modelBridgeOptions;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        SessionStore sessionStore,
        SessionTokenService tokenService,
        IOptions<ModelBridgeOptions> modelBridgeOptions,
        ILogger<SessionsController> logger)
    {
        _sessionStore = sessionStore;
        _tokenService = tokenService;
        _modelBridgeOptions = modelBridgeOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CreateSessionResponse>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        var modelName = string.IsNullOrWhiteSpace(request.ModelName) ? "default" : request.ModelName;

        var sessionId = Guid.CreateVersion7().ToString("N");

        var reserved = _sessionStore.CreateReservedSession(sessionId, modelName, _modelBridgeOptions.GrpcEndpoint);
 
        var token = _tokenService.IssueToken(reserved.SessionId);
        if (!_sessionStore.MarkActive(reserved.SessionId, token.Jti, token.ExpiresAtUtc))
        {
            _sessionStore.MarkFailed(reserved.SessionId, "Activation failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Session activation failed."
            });
        }

        _logger.LogInformation("Created session {SessionId} for model {ModelName}", reserved.SessionId, modelName);

        return Ok(new CreateSessionResponse
        {
            SessionId = reserved.SessionId,
            Token = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            WebSocketUrl = BuildWebSocketUrl(reserved.SessionId)
        });
    }

    private string BuildWebSocketUrl(string sessionId)
    {
        var uriBuilder = new UriBuilder
        {
            Scheme = Request.IsHttps ? "wss" : "ws",
            Host = Request.Host.Host,
            Path = _modelBridgeOptions.WebSocketPath,
            Query = $"sid={Uri.EscapeDataString(sessionId)}"
        };

        uriBuilder.Port = Request.Host.Port ?? -1;
        return uriBuilder.Uri.ToString();
    }
}
