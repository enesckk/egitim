using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Auth;
using EgitimPlatform.Modules.Identity.Configuration;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Identity.Features.Logout;
using EgitimPlatform.Modules.Identity.Features.Refresh;
using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.Modules.Identity.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace EgitimPlatform.Modules.Identity.Extensions;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // JWT settings
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Authentication
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

        // Authorization
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.CanManageStudents, policy =>
                policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin, Roles.Coach))
            .AddPolicy(Policies.CanViewStudents, policy =>
                policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin, Roles.Coach, Roles.Teacher, Roles.Parent))
            .AddPolicy(Policies.CanAssignCoach, policy =>
                policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin, Roles.Coach))
            .AddPolicy(Policies.CanManageInstitution, policy =>
                policy.RequireRole(Roles.SuperAdmin, Roles.InstitutionAdmin))
            .AddPolicy(Policies.CanViewAuditLogs, policy =>
                policy.RequireRole(Roles.SuperAdmin));

        // Auth services
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

    public static async Task SeedIdentityAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync(ct);
    }
}
