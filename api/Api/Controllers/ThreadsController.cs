using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/threads")]
public sealed class ThreadsController : ControllerBase
{
    private readonly IThreadGenerationService _threadGeneration;

    public ThreadsController(IThreadGenerationService threadGeneration)
    {
        _threadGeneration = threadGeneration;
    }

    // MVP: unauthenticated endpoint; client is identified by X-Client-Id
    [HttpPost("generate")]
    [EnableRateLimiting("threadgen")]
    [ProducesResponseType(typeof(GenerateThreadResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerateThreadResponseDto>> Generate(
        [FromBody] GenerateThreadRequestDto request,
        CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Client-Id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
        {
            clientId = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        var result = await _threadGeneration.GenerateAsync(request, clientId, cancellationToken);
        return Ok(result);
    }
}
