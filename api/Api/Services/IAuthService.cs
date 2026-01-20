using Api.Models.DTOs;

namespace Api.Services;

/// <summary>
/// Service interface for authentication operations.
/// Provides JWT token generation and validation.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and generates JWT tokens.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>Authentication response with tokens, or null if authentication fails.</returns>
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <returns>New authentication response with fresh tokens, or null if refresh fails.</returns>
    Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenRequestDto request);

    /// <summary>
    /// Generates a new JWT access token for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="roles">The user's roles.</param>
    /// <returns>JWT access token string.</returns>
    string GenerateAccessToken(string userId, string email, IEnumerable<string> roles);

    /// <summary>
    /// Generates a new refresh token.
    /// </summary>
    /// <returns>Refresh token string.</returns>
    string GenerateRefreshToken();
}
