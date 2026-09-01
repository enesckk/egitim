namespace EgitimPlatform.Modules.Identity.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? JwtId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByIp { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;
    public bool IsActive => !IsRevoked && !IsExpired;

    public virtual ApplicationUser User { get; set; } = null!;
}
