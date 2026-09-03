using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EgitimPlatform.Modules.Identity.Features.Refresh;

public class RefreshHandler
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<RefreshHandler> _logger;

    public RefreshHandler(
        IRefreshTokenService refreshTokenService,
        IJwtTokenService jwtTokenService,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings,
        ILogger<RefreshHandler> logger)
    {
        _refreshTokenService = refreshTokenService;
        _jwtTokenService = jwtTokenService;
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<RefreshResult?> HandleAsync(RefreshCommand command, string? ipAddress, CancellationToken ct = default)
    {
        var rotationResult = await _refreshTokenService.TryRotateAsync(command.RefreshToken, ipAddress, ct);

        if (rotationResult.Status != RotationStatus.Success)
        {
            _logger.LogInformation("Refresh rotation failed: {Status}", rotationResult.Status);
            return null;
        }

        if (rotationResult.NewStoredToken is null || rotationResult.NewRawToken is null)
            return null;

        // Generate new access token
        var user = await _userManager.FindByIdAsync(rotationResult.NewStoredToken.UserId.ToString());
        if (user is null || !user.IsActive || user.IsDeleted) return null;

        // P2-07: Check lockout status
        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogInformation("Refresh denied: user {UserId} is locked out", user.Id);
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (newAccessToken, _) = _jwtTokenService.GenerateAccessToken(user, roles);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        // P2-3: NewRawToken is returned only for cookie setting by the controller.
        // It is NOT included in the JSON response body.
        return new RefreshResult(newAccessToken, rotationResult.NewRawToken, expiresAt);
    }
}
