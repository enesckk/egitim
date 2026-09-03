using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EgitimPlatform.Modules.Identity.Infrastructure;

public class IdentitySeeder
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<IdentitySeeder> _logger;
    private readonly BootstrapSettings _bootstrapSettings;
    private readonly IWebHostEnvironment? _environment;

    public IdentitySeeder(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<IdentitySeeder> logger,
        IOptions<BootstrapSettings> bootstrapSettings,
        IWebHostEnvironment? environment = null)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _bootstrapSettings = bootstrapSettings.Value;
        _environment = environment;
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
    /// P2-03: All operations (user create + role assign + audit) are atomic within
    /// a single DB transaction. No PII in logs.
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

        // P2-03: Use explicit DB transaction for atomicity.
        // UserManager uses the same scoped ApplicationDbContext, so all operations
        // (CreateAsync, AddToRoleAsync) participate in this transaction.
        // If any step fails, everything rolls back — no partial state.
        // We need the concrete DbContext for BeginTransaction.
        var concreteContext = _context as Microsoft.EntityFrameworkCore.DbContext
            ?? throw new InvalidOperationException("Cannot begin transaction: unexpected DbContext type.");

        using var transaction = await concreteContext.Database.BeginTransactionAsync(ct);
        try
        {
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
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to bootstrap SuperAdmin: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                await transaction.RollbackAsync(ct);
                return;
            }

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.SuperAdmin);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Failed to assign SuperAdmin role: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                await transaction.RollbackAsync(ct);
                return;
            }

            // P2-03: Audit log entry — no password/secret. Email used as actor identifier
            // (configured credential, not PII in the traditional sense — it's the system
            // bootstrap account specified in configuration).
            var auditLog = new AuditLog
            {
                UserId = user.Id,
                Action = "Auth.Bootstrap.SuperAdmin.Created",
                EntityType = "ApplicationUser",
                EntityId = user.Id.ToString(),
                InstitutionId = null, // SuperAdmin has no institution
                MetadataJson = $"{{\"environment\":\"{_environment?.EnvironmentName ?? "unknown"}\"}}",
                Timestamp = DateTimeOffset.UtcNow,
            };
            _context.Set<AuditLog>().Add(auditLog);

            await concreteContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // P2-03: No PII in log — use userId only (email is a configured credential, not logged)
            _logger.LogCritical(
                "SuperAdmin bootstrap completed for userId={UserId}. " +
                "This is a privileged operation — ensure the password is changed immediately.",
                user.Id);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
