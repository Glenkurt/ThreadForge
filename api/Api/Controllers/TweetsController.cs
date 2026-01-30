using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

/// <summary>
/// Controller for single tweet operations including improvement.
/// </summary>
[ApiController]
[Route("api/v1/tweets")]
public sealed class TweetsController : ControllerBase
{
    private readonly ITweetImproverService _improverService;

    public TweetsController(ITweetImproverService improverService)
    {
        _improverService = improverService;
    }

    /// <summary>
    /// Improve an existing tweet draft using AI.
    /// </summary>
    /// <param name="request">The tweet draft and improvement options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Improved tweet with alternatives and explanation.</returns>
    /// <response code="200">Tweet improved successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="429">Rate limit exceeded.</response>
    /// <response code="500">Tweet improvement failed.</response>
    [HttpPost("improve")]
    [EnableRateLimiting("threadgen")]
    [ProducesResponseType(typeof(ImproveTweetResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ImproveTweetResponseDto>> Improve(
        [FromBody] ImproveTweetRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _improverService.ImproveAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto(ex.Message));
        }
    }

    /// <summary>
    /// Get available improvement types with descriptions.
    /// </summary>
    /// <returns>List of improvement types with their descriptions.</returns>
    [HttpGet("improvement-types")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, string>> GetImprovementTypes()
    {
        return Ok(ImprovementTypes.Descriptions);
    }
}
