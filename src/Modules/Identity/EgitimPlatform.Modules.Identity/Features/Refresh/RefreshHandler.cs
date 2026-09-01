using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace EgitimPlatform.Modules.Identity.Features.Refresh;

public class RefreshHandler
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public RefreshHandler(
        IRefreshTokenService refreshTokenService,
        IJwtTokenService jwtTokenService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _refreshTokenService = refreshTokenService;
        _jwtTokenService = jwtTokenService;
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<LoginResponseDto?> HandleAsync(RefreshCommand command, string? ipAddress, CancellationToken ct = default)
    {
        var existingToken = await _refreshTokenService.ValidateRefreshTokenAsync(command.RefreshToken, ct);
        if (existingToken is null) return null;

        // Revoke old refresh token (rotation)
        await _refreshTokenService.RevokeRefreshTokenAsync(existingToken, "Rotated", ipAddress, ct);

        // Generate new tokens
        var roles = await _userManager.GetRolesAsync(existingToken.User);
        var (newAccessToken, newJwtId) = _jwtTokenService.GenerateAccessToken(existingToken.User, roles);
        var newRefreshToken = _refreshTokenService.CreateRefreshToken(existingToken.UserId, newJwtId, ipAddress);

        existingToken.ReplacedByTokenId = newRefreshToken.Id;
        await _dbContext.SaveChangesAsync(ct);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        return new LoginResponseDto(newAccessToken, newRefreshToken.Token, expiresAt);
    }
}
