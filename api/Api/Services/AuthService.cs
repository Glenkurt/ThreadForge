using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Data;
using Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Api.Models.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

/// <summary>
/// JWT authentication service implementation.
/// Provides token generation and validation for API authentication.
/// 
/// NOTE: This is a scaffold implementation. In a real application:
/// - Validate credentials against a user store (database, Identity, etc.)
/// - Store refresh tokens securely (database, Redis, etc.)
/// - Implement proper token revocation
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public AuthService(
        IConfiguration configuration,
        ILogger<AuthService> logger,
        AppDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Task.FromResult<AuthResponseDto?>(null);
        }

        return LoginWithDatabaseAsync(request);
    }

    public Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Task.FromResult<AuthResponseDto?>(null);
        }

        return RefreshWithDatabaseAsync(request.RefreshToken);
    }

    public string GenerateAccessToken(string userId, string email, IEnumerable<string> roles)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret is not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "https://localhost";
        var audience = _configuration["Jwt:Audience"] ?? "https://localhost";
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task<AuthResponseDto?> LoginWithDatabaseAsync(LoginRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user is null)
        {
            return null;
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var roles = ParseRoles(user.Roles);
        var accessToken = GenerateAccessToken(user.Id.ToString(), user.Email, roles);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);
        var refreshExpiryDays = _configuration.GetValue<int>("Jwt:RefreshExpiryDays", 7);

        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays)
        };

        _dbContext.RefreshTokens.Add(refreshEntity);
        await _dbContext.SaveChangesAsync();

        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: expiryMinutes * 60,
            TokenType: "Bearer"
        );
    }

    private async Task<AuthResponseDto?> RefreshWithDatabaseAsync(string refreshToken)
    {
        var refreshTokenHash = HashToken(refreshToken);

        var tokenEntity = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == refreshTokenHash);

        if (tokenEntity is null || tokenEntity.User is null || !tokenEntity.IsActive)
        {
            return null;
        }

        tokenEntity.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = GenerateRefreshToken();
        var newRefreshTokenHash = HashToken(newRefreshToken);
        var refreshExpiryDays = _configuration.GetValue<int>("Jwt:RefreshExpiryDays", 7);

        var newRefreshEntity = new RefreshToken
        {
            UserId = tokenEntity.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays)
        };

        _dbContext.RefreshTokens.Add(newRefreshEntity);
        await _dbContext.SaveChangesAsync();

        var roles = ParseRoles(tokenEntity.User.Roles);
        var accessToken = GenerateAccessToken(tokenEntity.User.Id.ToString(), tokenEntity.User.Email, roles);
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryMinutes", 60);

        _logger.LogInformation("Token refreshed successfully for {Email}", tokenEntity.User.Email);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: newRefreshToken,
            ExpiresIn: expiryMinutes * 60,
            TokenType: "Bearer"
        );
    }

    private static IEnumerable<string> ParseRoles(string roles)
    {
        return roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
