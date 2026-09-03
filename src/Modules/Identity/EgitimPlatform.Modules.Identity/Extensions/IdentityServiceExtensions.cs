using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Identity.Features.Logout;
using EgitimPlatform.Modules.Identity.Features.Refresh;
using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.Modules.Identity.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace EgitimPlatform.Modules.Identity.Extensions;

/// <summary>
/// P2-12: Identity module service registration — no longer owns DbContext.
/// DbContext and Identity EF stores are registered by Infrastructure.AddPlatformInfrastructure().
/// This method registers: JWT auth, authorization policies, feature handlers, and Identity services.
/// </summary>
public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        // JWT settings — FAIL FAST on invalid/placeholder configuration
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");
        jwtSettings.Validate(); // Throws if key is missing, too short, or placeholder
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Bootstrap settings (SuperAdmin creation) — disabled by default
        services.Configure<BootstrapSettings>(configuration.GetSection(BootstrapSettings.SectionName));

        // Authentication (JWT Bearer)
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                ClockSkew = TimeSpan.FromMinutes(1),
            };
        });

        // Authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.CanManageStudents, policy =>
                policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin, Roles.Coach))
            .AddPolicy(Policies.CanViewStudents, policy =>
                policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin, Roles.Coach, Roles.Teacher, Roles.Parent))
            .AddPolicy(Policies.CanAssignCoach, policy =>
                policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin))
            .AddPolicy(Policies.CanManageInstitution, policy =>
                policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin))
            .AddPolicy(Policies.CanViewAuditLogs, policy =>
                policy.RequireRole(Roles.SuperAdmin));

        // Auth services — all use IApplicationDbContext (not concrete ApplicationDbContext)
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IInstitutionContext, InstitutionContext>();
        services.AddScoped<IAuditService, AuditService>();

        // Feature handlers
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<LogoutHandler>();

        // Validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Seeder
        services.AddScoped<IdentitySeeder>();

        // HttpContext accessor (needed for CurrentUser)
        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Seeds roles (deterministic, safe for all environments).
    /// Does NOT migrate the database — call Migrate separately if needed.
    /// Does NOT create SuperAdmin — bootstrap must be explicitly enabled.
    /// </summary>
    public static async Task SeedIdentityRolesAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedRolesAsync(ct);
    }

    /// <summary>
    /// Seeds the SuperAdmin bootstrap user ONLY if Bootstrap.Enabled = true
    /// and credentials are provided via secure configuration.
    /// Should only be called in Development/test environments.
    /// </summary>
    public static async Task SeedBootstrapSuperAdminAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedBootstrapSuperAdminAsync(ct);
    }
}
