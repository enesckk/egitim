using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Modules.Identity.Entities;

/// <summary>
/// Refresh token stored as a SHA-256 hash. The raw token value is NEVER persisted.
/// Supports token families for reuse detection (theft detection).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }

    /// <summary>
    /// SHA-256 hash of the raw token value. Used for lookup.
    /// Raw token is never stored.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Token family ID — groups related tokens (rotation chain).
    /// All tokens in a family are revoked together on reuse detection.
    /// </summary>
    public Guid FamilyId { get; set; }

    public string? JwtId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// When the token was consumed in a rotation. NULL if never used.
    /// If a token with UsedAt != null is presented again, it's reuse (theft).
    /// </summary>
    public DateTimeOffset? UsedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevocationReason { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Optimistic concurrency token — prevents concurrent rotation races.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    // Computed — not mapped
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;
    public bool IsActive => !IsRevoked && !IsExpired;

    public virtual ApplicationUser User { get; set; } = null!;
}

/// <summary>
/// Result of creating a new refresh token.
/// </summary>
public sealed record RefreshTokenCreationResult(
    RefreshToken StoredToken,
    string RawToken);
