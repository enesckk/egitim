using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Infrastructure;

namespace EgitimPlatform.Modules.Identity.Features.Logout;

public class LogoutHandler
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUser _currentUser;

    public LogoutHandler(IRefreshTokenService refreshTokenService, ICurrentUser currentUser)
    {
        _refreshTokenService = refreshTokenService;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(LogoutCommand command, string? ipAddress, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null) return;

        if (command.RefreshToken is not null)
        {
            // Revoke specific refresh token
            var token = await _refreshTokenService.ValidateRefreshTokenAsync(command.RefreshToken, ct);
            if (token is not null)
            {
                await _refreshTokenService.RevokeRefreshTokenAsync(token, "UserLogout", ipAddress, ct);
            }
        }
        else
        {
            // Revoke all tokens for this user
            await _refreshTokenService.RevokeAllUserTokensAsync(_currentUser.UserId.Value, ipAddress, ct);
        }
    }
}
