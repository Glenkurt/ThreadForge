using System.Text;
using Api.Services;
using Api.Models.Entities;
using Api.Models.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace Api.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to organize dependency injection registration.
/// Keeps Program.cs clean and groups related service registrations.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application services (business logic layer).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IThreadGenerationService, ThreadGenerationService>();
        services.AddScoped<IProfileAnalysisService, ProfileAnalysisService>();
        services.AddScoped<ITweetImproverService, TweetImproverService>();
        services.AddSingleton<IThreadQualityService, ThreadQualityService>();

        return services;
    }

    public static IServiceCollection AddXai(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["Xai:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Xai:ApiKey must be configured in appsettings, environment variables, or user secrets");
        }

        services.Configure<XaiOptions>(configuration.GetSection(XaiOptions.SectionName));

        services.AddHttpClient<IXaiChatClient, XaiChatClient>();

        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret must be configured in appsettings or environment variables");

        var jwtIssuer = configuration["Jwt:Issuer"] ?? "https://localhost";
        var jwtAudience = configuration["Jwt:Audience"] ?? "https://localhost";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Append("X-Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Configures rate limiting policies.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Fixed window policy for general API endpoints
            options.AddPolicy("fixed", context =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Stricter policy for authentication endpoints
            options.AddPolicy("auth", context =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // MVP: 20 thread generations per anonymous user per day
            // Security: Use IP + X-Client-Id combination to prevent header spoofing bypass
            options.AddPolicy("threadgen", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var clientId = context.Request.Headers["X-Client-Id"].ToString();

                // Combine IP and client ID for rate limiting key
                // This prevents bypassing by just changing the X-Client-Id header
                string partitionKey;
                if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
                {
                    partitionKey = ipAddress;
                }
                else
                {
                    // Combine IP + ClientId so changing client ID alone doesn't bypass limits
                    partitionKey = $"{ipAddress}:{clientId}";
                }

                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromDays(1),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
