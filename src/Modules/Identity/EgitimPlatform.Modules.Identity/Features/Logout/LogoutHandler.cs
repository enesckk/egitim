using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Identity.Features.Logout;

public class LogoutHandler
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUser _currentUser;
    private readonly ApplicationDbContext _dbContext;

    public LogoutHandler(
        IRefreshTokenService refreshTokenService,
        ICurrentUser currentUser,
        ApplicationDbContext dbContext)
    {
        _refreshTokenService = refreshTokenService;
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    public async Task HandleAsync(LogoutCommand command, string? ipAddress, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null) return;

        if (command.RefreshToken is not null)
        {
            // P2-01: Ownership check — only revoke own tokens.
            // Hash the incoming token to find it, verify UserId matches.
            var tokenHash = RefreshTokenService.HashToken(command.RefreshToken);
            var token = await _dbContext.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

            if (token is not null && token.UserId == _currentUser.UserId.Value)
            {
                // P2-01: Don't leak token existence — generic response regardless
                await _refreshTokenService.TryRevokeByRawTokenAsync(command.RefreshToken, "UserLogout", ipAddress, ct);
            }
            // If token doesn't exist or belongs to another user → silently no-op
            // (don't reveal whether the token exists or who owns it)
        }
        else
        {
            // Revoke all tokens for the current user
            await _refreshTokenService.RevokeAllUserTokensAsync(_currentUser.UserId.Value, "UserLogout", ipAddress, ct);
        }
    }
}
