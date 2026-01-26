using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

/// <summary>
/// Controller for Twitter profile analysis and brand description generation.
/// </summary>
[ApiController]
[Route("api/v1/profiles")]
public sealed class ProfilesController : ControllerBase
{
    private readonly IProfileAnalysisService _profileAnalysis;

    public ProfilesController(IProfileAnalysisService profileAnalysis)
    {
        _profileAnalysis = profileAnalysis;
    }

    /// <summary>
    /// Analyze a Twitter profile and generate a brand description document.
    /// </summary>
    /// <param name="request">Profile analysis request containing the Twitter username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comprehensive brand description including voice, audience, and content strategy.</returns>
    /// <response code="200">Profile analyzed successfully.</response>
    /// <response code="400">Invalid username format or insufficient tweets.</response>
    /// <response code="404">Twitter profile not found.</response>
    /// <response code="429">Rate limit exceeded.</response>
    /// <response code="503">Unable to access Twitter data.</response>
    [HttpPost("analyze")]
    [EnableRateLimiting("threadgen")]
    [ProducesResponseType(typeof(ProfileAnalysisResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ProfileAnalysisResponseDto>> Analyze(
        [FromBody] ProfileAnalysisRequestDto request,
        CancellationToken cancellationToken)
    {
        var clientId = Request.Headers["X-Client-Id"].ToString();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        try
        {
            var result = await _profileAnalysis.AnalyzeAsync(request, clientId, cancellationToken);
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
}
