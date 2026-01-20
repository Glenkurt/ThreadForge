namespace Api.Models.Entities;

/// <summary>
/// Minimal user entity for authentication scaffolding.
/// Replace/extend with full identity model as needed.
/// </summary>
public class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated role list for template simplicity.
    /// Replace with a normalized role table for production.
    /// </summary>
    public string Roles { get; set; } = "User";

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
