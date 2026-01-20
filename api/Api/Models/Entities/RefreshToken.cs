namespace Api.Models.Entities;

/// <summary>
/// Refresh token entity stored as a hash for security.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public ApplicationUser? User { get; set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow <= ExpiresAt;
}
