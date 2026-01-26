using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

/// <summary>
/// Controller for AI-powered Twitter thread generation.
/// </summary>
[ApiController]
[Route("api/v1/threads")]
public sealed class ThreadsController : ControllerBase
{
    private readonly IThreadGenerationService _threadGeneration;

    public ThreadsController(IThreadGenerationService threadGeneration)
    {
        _threadGeneration = threadGeneration;
    }

    /// <summary>
    /// Generate an AI-powered Twitter thread based on topic and parameters.
    /// </summary>
    /// <param name="request">Thread generation parameters including topic, tone, and tweet count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated thread with tweets and metadata.</returns>
    /// <response code="200">Thread generated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="429">Rate limit exceeded.</response>
    /// <response code="500">Thread generation failed.</response>
    // MVP: unauthenticated endpoint; client is identified by X-Client-Id
    [HttpPost("generate")]
    [EnableRateLimiting("threadgen")]
    [ProducesResponseType(typeof(GenerateThreadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
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
