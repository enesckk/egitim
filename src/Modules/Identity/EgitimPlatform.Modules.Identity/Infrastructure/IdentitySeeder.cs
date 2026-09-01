using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EgitimPlatform.Modules.Identity.Infrastructure;

public class IdentitySeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<IdentitySeeder> _logger;
    private readonly BootstrapSettings _bootstrapSettings;

    public IdentitySeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<IdentitySeeder> logger,
        IOptions<BootstrapSettings> bootstrapSettings)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _bootstrapSettings = bootstrapSettings.Value;
    }

    /// <summary>
    /// Executes all standard seeding operations.
    /// </summary>
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct);
        await SeedBootstrapSuperAdminAsync(ct);
    }

    /// <summary>
    /// Seeds roles (deterministic, safe for all environments).
    /// Does NOT migrate the database — migrations must be run explicitly.
    /// </summary>
    public async Task SeedRolesAsync(CancellationToken ct = default)
    {
        foreach (var roleName in Roles.All)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole { Name = roleName, Description = $"{roleName} role" };
                var result = await _roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create role {Role}: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    /// <summary>
    /// Seeds the initial SuperAdmin user ONLY if bootstrap is explicitly enabled
    /// with valid credentials. Never creates a user with hardcoded/default credentials.
    /// Logs a critical audit entry on success. Never logs the password.
    /// </summary>
    public async Task SeedBootstrapSuperAdminAsync(CancellationToken ct = default)
    {
        if (!_bootstrapSettings.Enabled)
        {
            _logger.LogDebug("SuperAdmin bootstrap is disabled. Skipping.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_bootstrapSettings.SuperAdminEmail) ||
            string.IsNullOrWhiteSpace(_bootstrapSettings.SuperAdminPassword))
        {
            throw new InvalidOperationException(
                "Bootstrap is enabled but SuperAdminEmail or SuperAdminPassword is not configured. " +
                "Provide credentials via environment variables or secret manager.");
        }

        var email = _bootstrapSettings.SuperAdminEmail;

        if (await _userManager.Users.AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), ct))
        {
            _logger.LogDebug("SuperAdmin user already exists. Skipping bootstrap.");
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Bootstrap",
            LastName = "Admin",
            IsActive = true,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, _bootstrapSettings.SuperAdminPassword);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, Roles.SuperAdmin);
            _logger.LogCritical(
                "SuperAdmin bootstrap completed for {Email}. " +
                "This is a privileged operation — ensure the password is changed immediately.",
                email);
        }
        else
        {
            _logger.LogError("Failed to bootstrap SuperAdmin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
