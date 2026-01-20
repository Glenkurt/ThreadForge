namespace Api.Models.DTOs;

/// <summary>
/// Response DTO for health check endpoint.
/// Uses C# record for immutability and built-in equality.
/// </summary>
/// <param name="Status">Overall health status: "ok", "degraded", or "unhealthy".</param>
/// <param name="Timestamp">UTC timestamp of the health check.</param>
/// <param name="Database">Database connection status.</param>
/// <param name="Version">API version string.</param>
public record HealthResponseDto(
    string Status,
    DateTime Timestamp,
    string Database,
    string Version
);
