using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EgitimPlatform.Modules.Identity.Features.Login;

public class LoginHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ApplicationDbContext dbContext,
        IAuditService auditService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<LoginHandler> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _dbContext = dbContext;
        _auditService = auditService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Result of login attempt — distinguishes failure reasons for audit
    /// without leaking information to the caller.
    /// </summary>
    public sealed record LoginAttemptResult(
        LoginStatus Status,
        LoginResponseDto? Tokens = null);

    public enum LoginStatus
    {
        Success,
        InvalidCredentials,
        UserLockedOut,
        UserNotActive,
        UserNotFound,
    }

    public async Task<LoginAttemptResult> HandleAsync(LoginCommand command, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == command.Email.ToUpperInvariant() && !u.IsDeleted, ct);

        if (user is null)
        {
            // Audit failed attempt without user ID (use a sentinel for unknown user)
            _logger.LogInformation("Login failed: user not found for {Email}", command.Email);
            return new LoginAttemptResult(LoginStatus.UserNotFound);
        }

        if (!user.IsActive)
        {
            _logger.LogInformation("Login failed: user {UserId} is not active", user.Id);
            return new LoginAttemptResult(LoginStatus.UserNotActive);
        }

        // Use CheckPasswordSignInAsync to enable lockout on failure.
        // lockEnabled: true → increments AccessFailedCount and locks out after threshold.
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            _logger.LogWarning("Login failed: user {UserId} is locked out", user.Id);
            await AuditLoginAsync(user, "Auth.Login.LockedOut", ipAddress, ct);
            return new LoginAttemptResult(LoginStatus.UserLockedOut);
        }

        if (signInResult.IsNotAllowed)
        {
            _logger.LogInformation("Login failed: user {UserId} sign-in not allowed", user.Id);
            return new LoginAttemptResult(LoginStatus.UserNotActive);
        }

        if (!signInResult.Succeeded)
        {
            _logger.LogInformation("Login failed: invalid password for user {UserId}", user.Id);
            await AuditLoginAsync(user, "Auth.Login.Failed", ipAddress, ct);
            return new LoginAttemptResult(LoginStatus.InvalidCredentials);
        }

        // Success — reset access failed count is handled automatically by SignInManager
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, jwtId) = _jwtTokenService.GenerateAccessToken(user, roles);

        var refreshResult = _refreshTokenService.CreateRefreshToken(user.Id, jwtId, ipAddress);
        await _dbContext.SaveChangesAsync(ct);

        await AuditLoginAsync(user, "Auth.Login.Success", ipAddress, ct);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        return new LoginAttemptResult(
            LoginStatus.Success,
            new LoginResponseDto(accessToken, refreshResult.RawToken, expiresAt));
    }

    private async Task AuditLoginAsync(ApplicationUser user, string action, string? ipAddress, CancellationToken ct)
    {
        await _auditService.LogAsync(
            userId: user.Id,
            action: action,
            entityType: "User",
            entityId: user.Id.ToString(),
            institutionId: user.InstitutionId,
            ipAddress: ipAddress,
            cancellationToken: ct);
    }
}
