using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Identity.Features.Login;

public class LoginHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;

    public LoginHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ApplicationDbContext dbContext,
        IAuditService auditService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async Task<LoginResponseDto?> HandleAsync(LoginCommand command, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == command.Email.ToUpperInvariant() && !u.IsDeleted, ct);

        if (user is null || !user.IsActive)
            return null;

        var passwordValid = await _userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordValid)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, jwtId) = _jwtTokenService.GenerateAccessToken(user, roles);

        var refreshToken = _refreshTokenService.CreateRefreshToken(user.Id, jwtId, ipAddress);
        await _dbContext.SaveChangesAsync(ct);

        await _auditService.LogAsync(
            userId: user.Id,
            action: "Auth.Login",
            entityType: "User",
            entityId: user.Id.ToString(),
            institutionId: user.InstitutionId,
            ipAddress: ipAddress,
            cancellationToken: ct);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30); // Should come from settings
        return new LoginResponseDto(accessToken, refreshToken.Token, expiresAt);
    }
}
