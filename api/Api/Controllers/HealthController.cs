using Api.Models.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

/// <summary>
/// Health check endpoint for monitoring and container orchestration.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("fixed")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    /// <summary>
    /// Performs a comprehensive health check including database connectivity.
    /// </summary>
    /// <returns>Health status with component details.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var health = await _healthService.CheckHealthAsync(cancellationToken);

        // Return 503 if unhealthy for proper load balancer detection
        if (health.Status == "unhealthy")
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, health);
        }

        return Ok(health);
    }
}
