using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace EgitimPlatform.Modules.Identity.Auth;

public interface IRefreshTokenService
{
    RefreshToken CreateRefreshToken(Guid userId, string jwtId, string? ipAddress);
    Task<RefreshToken?> ValidateRefreshTokenAsync(string token, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(RefreshToken token, string? reason, string? ipAddress, CancellationToken ct = default);
    Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress, CancellationToken ct = default);
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _settings;

    public RefreshTokenService(ApplicationDbContext context, IOptions<JwtSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    public RefreshToken CreateRefreshToken(Guid userId, string jwtId, string? ipAddress)
    {
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = tokenValue,
            JwtId = jwtId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenExpirationDays),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress,
        };

        _context.RefreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (refreshToken is null) return null;
        if (!refreshToken.IsActive) return null;
        if (refreshToken.User is null || !refreshToken.User.IsActive || refreshToken.User.IsDeleted) return null;

        return refreshToken;
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken token, string? reason, string? ipAddress, CancellationToken ct = default)
    {
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.RevokedByIp = ipAddress;
        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress, CancellationToken ct = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await _context.SaveChangesAsync(ct);
    }
}
