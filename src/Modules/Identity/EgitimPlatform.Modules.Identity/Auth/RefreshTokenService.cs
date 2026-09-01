using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace EgitimPlatform.Modules.Identity.Auth;

public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token. Returns the stored entity (hash) AND the raw token
    /// (to be sent to the client once, then discarded).
    /// </summary>
    RefreshTokenCreationResult CreateRefreshToken(Guid userId, string jwtId, string? ipAddress, Guid? familyId = null);

    /// <summary>
    /// Atomically rotates a refresh token:
    /// 1. Look up by hash
    /// 2. If not found → null (invalid token)
    /// 3. If already used → REUSE DETECTED → revoke entire family
    /// 4. If expired → revoke and return null
    /// 5. Mark current as used + revoked, create new token in same family
    /// 6. Return new raw token
    /// </summary>
    Task<RefreshTokenRotationResult> TryRotateAsync(string rawToken, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Revokes a single token by raw value (hash lookup).
    /// </summary>
    Task<bool> TryRevokeByRawTokenAsync(string rawToken, string? reason, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Revokes all active tokens for a user.
    /// </summary>
    Task RevokeAllUserTokensAsync(Guid userId, string? reason, string? ipAddress, CancellationToken ct = default);
}

public sealed record RefreshTokenRotationResult(
    RotationStatus Status,
    RefreshToken? NewStoredToken = null,
    string? NewRawToken = null);

public enum RotationStatus
{
    Success,
    TokenNotFound,
    TokenExpired,
    TokenAlreadyRevoked,
    ReuseDetected,
    UserInactive,
    ConcurrencyConflict,
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _settings;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        ApplicationDbContext context,
        IOptions<JwtSettings> settings,
        ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public RefreshTokenCreationResult CreateRefreshToken(Guid userId, string jwtId, string? ipAddress, Guid? familyId = null)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = HashToken(rawToken);
        var family = familyId ?? Guid.CreateVersion7();

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = family,
            JwtId = jwtId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenExpirationDays),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress,
        };

        _context.RefreshTokens.Add(refreshToken);
        return new RefreshTokenCreationResult(refreshToken, rawToken);
    }

    public async Task<RefreshTokenRotationResult> TryRotateAsync(string rawToken, string? ipAddress, CancellationToken ct = default)
    {
        var hash = HashToken(rawToken);

        // Find the token by hash
        var existingToken = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existingToken is null)
        {
            return new RefreshTokenRotationResult(RotationStatus.TokenNotFound);
        }

        // Check user status
        if (existingToken.User is null || !existingToken.User.IsActive || existingToken.User.IsDeleted)
        {
            return new RefreshTokenRotationResult(RotationStatus.UserInactive);
        }

        // REUSE DETECTION: if token was already consumed, this is theft
        if (existingToken.UsedAt.HasValue)
        {
            _logger.LogWarning("Refresh token reuse detected for family {FamilyId}. Revoking entire family.", existingToken.FamilyId);
            await RevokeFamilyAsync(existingToken.FamilyId, "ReuseDetected", ipAddress, ct);
            return new RefreshTokenRotationResult(RotationStatus.ReuseDetected);
        }

        // Already revoked (but never used) — just deny
        if (existingToken.IsRevoked)
        {
            return new RefreshTokenRotationResult(RotationStatus.TokenAlreadyRevoked);
        }

        // Expired
        if (existingToken.IsExpired)
        {
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            existingToken.RevokedByIp = ipAddress;
            existingToken.RevocationReason = "Expired";
            await _context.SaveChangesAsync(ct);
            return new RefreshTokenRotationResult(RotationStatus.TokenExpired);
        }

        // ATOMIC ROTATION:
        // 1. Mark current token as used + revoked
        // 2. Create new token in same family
        // 3. Save atomically
        try
        {
            existingToken.UsedAt = DateTimeOffset.UtcNow;
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            existingToken.RevokedByIp = ipAddress;
            existingToken.RevocationReason = "Rotated";

            var newResult = CreateRefreshToken(existingToken.UserId, existingToken.JwtId!, ipAddress, existingToken.FamilyId);
            existingToken.ReplacedByTokenId = newResult.StoredToken.Id;

            await _context.SaveChangesAsync(ct);

            return new RefreshTokenRotationResult(
                RotationStatus.Success,
                newResult.StoredToken,
                newResult.RawToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict during refresh token rotation for family {FamilyId}", existingToken.FamilyId);
            return new RefreshTokenRotationResult(RotationStatus.ConcurrencyConflict);
        }
    }

    public async Task<bool> TryRevokeByRawTokenAsync(string rawToken, string? reason, string? ipAddress, CancellationToken ct = default)
    {
        var hash = HashToken(rawToken);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || token.IsRevoked) return false;

        token.RevokedAt = DateTimeOffset.UtcNow;
        token.RevokedByIp = ipAddress;
        token.RevocationReason = reason;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? reason, string? ipAddress, CancellationToken ct = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = ipAddress;
            token.RevocationReason = reason;
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task RevokeFamilyAsync(Guid familyId, string reason, string? ipAddress, CancellationToken ct)
    {
        var familyTokens = await _context.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in familyTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = ipAddress;
            token.RevocationReason = reason;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static string GenerateRawToken()
    {
        // 64 bytes = 512 bits of entropy → base64url for transport safety
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    internal static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
