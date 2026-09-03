using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Entities;

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
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public LoginHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IApplicationDbContext dbContext,
        IAuditService auditService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<LoginHandler> logger,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _dbContext = dbContext;
        _auditService = auditService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Result of login attempt — distinguishes failure reasons for audit
    /// without leaking information to the caller.
    /// </summary>
    public sealed record LoginAttemptResult(
        LoginStatus Status,
        LoginResponseDto? Tokens = null,
        string? RawRefreshToken = null);

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
            // P2-2: Perform a dummy password hash to prevent timing-based user enumeration.
            // Without this, the "user not found" path returns significantly faster than the
            // "wrong password" path, allowing attackers to determine valid emails.
            // We hash against a throwaway user object — the result is discarded.
            var dummyUser = new ApplicationUser { Id = Guid.Empty };
            _passwordHasher.HashPassword(dummyUser, command.Password);

            // P2-1: No PII (email/password) in logs — use safe event codes.
            _logger.LogInformation("Login failed: reason={Reason}, ip={IpAddress}",
                "UserNotFound", ipAddress ?? "unknown");
            return new LoginAttemptResult(LoginStatus.UserNotFound);
        }

        if (!user.IsActive)
        {
            // P2-2: Still verify password hash even for inactive users to maintain constant timing.
            // Result is intentionally not awaited — we discard it to prevent timing difference.
            _ = _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: false);

            _logger.LogInformation("Login failed: reason={Reason}, userId={UserId}, ip={IpAddress}",
                "UserNotActive", user.Id, ipAddress ?? "unknown");
            return new LoginAttemptResult(LoginStatus.UserNotActive);
        }

        // Use CheckPasswordSignInAsync to enable lockout on failure.
        // lockEnabled: true → increments AccessFailedCount and locks out after threshold.
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            _logger.LogWarning("Login failed: reason={Reason}, userId={UserId}, ip={IpAddress}",
                "UserLockedOut", user.Id, ipAddress ?? "unknown");
            // P2-9: Audit uses AddPendingLogAsync + SaveChanges for atomicity
            await _auditService.AddPendingLogAsync(
                userId: user.Id,
                action: "Auth.Login.LockedOut",
                entityType: "User",
                entityId: user.Id.ToString(),
                institutionId: user.InstitutionId,
                ipAddress: ipAddress);
            await _dbContext.SaveChangesAsync(ct);
            return new LoginAttemptResult(LoginStatus.UserLockedOut);
        }

        if (signInResult.IsNotAllowed)
        {
            _logger.LogInformation("Login failed: reason={Reason}, userId={UserId}, ip={IpAddress}",
                "SignInNotAllowed", user.Id, ipAddress ?? "unknown");
            return new LoginAttemptResult(LoginStatus.UserNotActive);
        }

        if (!signInResult.Succeeded)
        {
            _logger.LogInformation("Login failed: reason={Reason}, userId={UserId}, ip={IpAddress}",
                "InvalidPassword", user.Id, ipAddress ?? "unknown");
            await _auditService.AddPendingLogAsync(
                userId: user.Id,
                action: "Auth.Login.Failed",
                entityType: "User",
                entityId: user.Id.ToString(),
                institutionId: user.InstitutionId,
                ipAddress: ipAddress);
            await _dbContext.SaveChangesAsync(ct);
            return new LoginAttemptResult(LoginStatus.InvalidCredentials);
        }

        // Success — reset access failed count is handled automatically by SignInManager
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, jwtId) = _jwtTokenService.GenerateAccessToken(user, roles);

        var refreshResult = _refreshTokenService.CreateRefreshToken(user.Id, jwtId, ipAddress);

        // P2-9: Atomic — refresh token + audit log in single SaveChanges.
        // Previously, refresh token was saved first, then audit was a separate SaveChanges.
        // If the audit save failed, the token was already persisted — inconsistent state.
        await _auditService.AddPendingLogAsync(
            userId: user.Id,
            action: "Auth.Login.Success",
            entityType: "User",
            entityId: user.Id.ToString(),
            institutionId: user.InstitutionId,
            ipAddress: ipAddress);

        await _dbContext.SaveChangesAsync(ct);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        return new LoginAttemptResult(
            LoginStatus.Success,
            new LoginResponseDto(accessToken, expiresAt),
            RawRefreshToken: refreshResult.RawToken);
    }
}
