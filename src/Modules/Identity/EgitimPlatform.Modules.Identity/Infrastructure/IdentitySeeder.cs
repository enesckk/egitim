using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EgitimPlatform.Modules.Identity.Infrastructure;

public class IdentitySeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<IdentitySeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _context.Database.MigrateAsync(ct);
        await SeedRolesAsync(ct);
        await SeedSuperAdminAsync(ct);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
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
                else
                {
                    _logger.LogInformation("Created role: {Role}", roleName);
                }
            }
        }
    }

    private async Task SeedSuperAdminAsync(CancellationToken ct)
    {
        const string superAdminEmail = "superadmin@egitimplatform.local";
        const string superAdminPassword = "SuperAdmin@123!"; // Development only — change in production

        if (await _userManager.Users.AnyAsync(u => u.Email == superAdminEmail, ct))
            return;

        var user = new ApplicationUser
        {
            UserName = superAdminEmail,
            Email = superAdminEmail,
            FirstName = "Super",
            LastName = "Admin",
            IsActive = true,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, superAdminPassword);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, Roles.SuperAdmin);
            _logger.LogInformation("Created SuperAdmin user: {Email}", superAdminEmail);
        }
        else
        {
            _logger.LogError("Failed to create SuperAdmin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
