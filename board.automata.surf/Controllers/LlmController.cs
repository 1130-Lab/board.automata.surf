using board.automata.surf.api.models;
using Microsoft.AspNetCore.Mvc;

namespace board.automata.surf.controllers;

/* ~~~~~~~~~~~~~~~~~~~~~~~~~
 * We replicate the API of Ollama, giving us the ability 
 * to impersonate Ollama for existing tooling.
 * ~~~~~~~~~~~~~~~~~~~~~~~~~ */

[ApiController]
[Route("api")]
public sealed class LlmController : ControllerBase
{
  private readonly ILogger<LlmController> _logger;

  public LlmController(ILogger<LlmController> logger)
  {
    _logger = logger;
  }

  [HttpPost("generate")]
  public ActionResult<OllamaGenerateResponse> Generate([FromBody] OllamaGenerateRequest request)
  {

    return EndpointNotImplemented("generate");
  }

  [HttpPost("chat")]
  public ActionResult<OllamaChatResponse> Chat([FromBody] OllamaChatRequest request)
  {
    return EndpointNotImplemented("chat");
  }

  [HttpPost("create")]
  public ActionResult<OllamaStatusResponse> Create([FromBody] OllamaCreateModelRequest request)
  {
    return EndpointNotImplemented("create");
  }

  [HttpHead("blobs/{digest}")]
  public IActionResult HeadBlob([FromRoute] string digest)
  {
    return EndpointNotImplemented("blobs/{digest}");
  }

  [HttpPost("blobs/{digest}")]
  public IActionResult CreateBlob([FromRoute] string digest)
  {
    return EndpointNotImplemented("blobs/{digest}");
  }

  [HttpGet("tags")]
  public ActionResult<OllamaTagsResponse> Tags()
  {
    return EndpointNotImplemented("tags");
  }

  [HttpPost("show")]
  public ActionResult<OllamaShowResponse> Show([FromBody] OllamaShowRequest request)
  {
    return EndpointNotImplemented("show");
  }

  [HttpPost("copy")]
  public IActionResult Copy([FromBody] OllamaCopyModelRequest request)
  {
    return EndpointNotImplemented("copy");
  }

  [HttpDelete("delete")]
  public IActionResult Delete([FromBody] OllamaDeleteModelRequest request)
  {
    return EndpointNotImplemented("delete");
  }

  [HttpPost("pull")]
  public ActionResult<OllamaStatusResponse> Pull([FromBody] OllamaPullModelRequest request)
  {
    return EndpointNotImplemented("pull");
  }

  [HttpPost("push")]
  public ActionResult<OllamaStatusResponse> Push([FromBody] OllamaPushModelRequest request)
  {
    return EndpointNotImplemented("push");
  }

  [HttpPost("embed")]
  public ActionResult<OllamaEmbedResponse> Embed([FromBody] OllamaEmbedRequest request)
  {
    return EndpointNotImplemented("embed");
  }

  [HttpGet("ps")]
  public ActionResult<OllamaPsResponse> Ps()
  {
    return EndpointNotImplemented("ps");
  }

  [HttpPost("embeddings")]
  public ActionResult<OllamaEmbeddingsResponse> Embeddings([FromBody] OllamaEmbeddingsRequest request)
  {
    return EndpointNotImplemented("embeddings");
  }

  [HttpGet("version")]
  public ActionResult<OllamaVersionResponse> Version()
  {
    return EndpointNotImplemented("version");
  }

  private ObjectResult EndpointNotImplemented(string endpoint)
  {
    _logger.LogInformation("Ollama-compatible endpoint {Endpoint} is not implemented yet.", endpoint);
    return StatusCode(StatusCodes.Status501NotImplemented, new
    {
      message = $"The Ollama-compatible endpoint '/api/{endpoint}' is not implemented yet."
    });
  }
}
