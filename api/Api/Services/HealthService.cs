using Api.Data;
using Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Implementation of health check service.
/// Encapsulates health check logic away from the controller.
/// </summary>
public class HealthService : IHealthService
{
    private readonly AppDbContext _context;
    private readonly ILogger<HealthService> _logger;

    public HealthService(AppDbContext context, ILogger<HealthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthResponseDto> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var databaseStatus = await CheckDatabaseAsync(cancellationToken);

        var overallStatus = databaseStatus == "connected" ? "ok" : "unhealthy";

        _logger.LogInformation(
            "Health check completed. Status: {Status}, Database: {DatabaseStatus}",
            overallStatus,
            databaseStatus);

        return new HealthResponseDto(
            Status: overallStatus,
            Timestamp: DateTime.UtcNow,
            Database: databaseStatus,
            Version: GetApiVersion()
        );
    }

    private async Task<string> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            return canConnect ? "connected" : "disconnected";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            return "disconnected";
        }
    }

    private static string GetApiVersion()
    {
        return typeof(HealthService).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }
}
