using Api.Models.DTOs;

namespace Api.Services;

/// <summary>
/// Service interface for health check operations.
/// Demonstrates the service layer pattern for dependency injection and testability.
/// </summary>
public interface IHealthService
{
    /// <summary>
    /// Performs a comprehensive health check including database connectivity.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Health status response with component statuses.</returns>
    Task<HealthResponseDto> CheckHealthAsync(CancellationToken cancellationToken = default);
}
