using EgitimPlatform.Api.Extensions;
using EgitimPlatform.Api.Middleware;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Coaching.Extensions;
using EgitimPlatform.Modules.Identity.Extensions;
using EgitimPlatform.Modules.Students.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "EgitimPlatform.Api")
        .WriteTo.Console());

    // P2-12: Platform persistence (DbContext, Identity EF stores) — neutral Infrastructure layer.
    // Must be registered before Identity module (which configures JWT, policies, handlers).
    builder.Services.AddPlatformInfrastructure(builder.Configuration);

    // Identity module (JWT auth, authorization policies, feature handlers)
    builder.Services.AddIdentityModule(builder.Configuration);

    // Domain modules
    builder.Services.AddStudentsModule();
    builder.Services.AddCoachingModule();

    // Controllers
    builder.Services.AddControllers();

    // ProblemDetails
    builder.Services.AddProblemDetails();

    // Swagger with JWT auth support
    builder.Services.AddSwaggerWithAuth();

    // P2-3: CORS — configurable origins, NOT AllowAny in production.
    // Reads "CorsSettings:AllowedOrigins" from configuration.
    // Falls back to AllowAnyOrigin + AllowAnyHeader (no credentials) only in Development.
    var allowedOrigins = builder.Configuration
        .GetSection("CorsSettings:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                policy.SetIsOriginAllowed(_ => false)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        });
    });

    // P2-02 CLOSURE: CSRF origin validation for state-changing auth endpoints.
    // Uses the same allowed origins as CORS for consistency.
    builder.Services.Configure<CsrfOriginOptions>(options =>
    {
        options.AllowedOrigins = allowedOrigins;
        options.AllowAllInDevelopment = builder.Environment.IsDevelopment();
    });

    // P1-02 CLOSURE: ForwardedHeaders — configuration-driven trust model.
    // Reverse proxy must be explicitly enabled with known proxy configuration.
    // Never trust forwarded headers from the entire internet.
    // Without this, attackers can spoof X-Forwarded-For to bypass rate limiting.
    var reverseProxySection = builder.Configuration.GetSection("ReverseProxy");
    var proxyEnabled = reverseProxySection.GetValue<bool>("Enabled", false);

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                 | ForwardedHeaders.XForwardedProto
                                 | ForwardedHeaders.XForwardedHost;

        if (proxyEnabled)
        {
            // P1-02: Bounded forward limit (default 1 for single reverse proxy).
            // Prevents unbounded header chain traversal.
            options.ForwardLimit = reverseProxySection.GetValue<int>("ForwardLimit", 1);

            // Configure trusted proxies from configuration
            var knownProxies = reverseProxySection.GetSection("KnownProxies").Get<string[]>();

            if (knownProxies is { Length: > 0 })
            {
                options.KnownProxies.Clear();
                foreach (var proxyIp in knownProxies)
                {
                    if (System.Net.IPAddress.TryParse(proxyIp, out var address))
                        options.KnownProxies.Add(address);
                }
            }

            // P1-02: Configure trusted proxies from configuration.
            // KnownProxies uses IPAddress — simple and sufficient for most deployments.
            if (knownProxies is { Length: > 0 })
            {
                options.KnownProxies.Clear();
                foreach (var proxyIp in knownProxies)
                {
                    if (System.Net.IPAddress.TryParse(proxyIp, out var address))
                        options.KnownProxies.Add(address);
                }
            }

            // P1-02: If proxy is enabled but no trusted proxies configured,
            // fail safely — only trust localhost loopback (development scenario).
            if (knownProxies is null || knownProxies.Length == 0)
            {
                options.KnownProxies.Clear();
                options.KnownProxies.Add(System.Net.IPAddress.Loopback);
                options.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
            }
        }
        else
        {
            // No reverse proxy — do NOT process any forwarded headers.
            // Direct connections: use actual connection info only.
            options.ForwardedHeaders = ForwardedHeaders.None;
        }
    });

    // OpenAPI
    builder.Services.AddEndpointsApiExplorer();

    // P2-01 CLOSURE: Rate limiting for auth endpoints.
    // Configurable via RateLimiting:LoginLimit and RateLimiting:RefreshLimit.
    // Config is read at REQUEST TIME (not registration time) so test overrides work.
    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("login", httpContext =>
        {
            var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            var limit = config.GetValue<int>("RateLimiting:LoginLimit", 10);
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
        });

        options.AddPolicy("refresh", httpContext =>
        {
            var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            var limit = config.GetValue<int>("RateLimiting:RefreshLimit", 30);
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    var app = builder.Build();

    // P2-06 CLOSURE: Explicit production initialization mode.
    // Usage: dotnet EgitimPlatform.Api.dll --initialize-platform
    // This runs migration + role seed + optional bootstrap, then exits.
    // Does NOT start the web server. Idempotent — safe to run multiple times.
    // Only triggered by explicit command-line argument — never by config or test harness.
    if (args.Length > 0 && args.Contains("--initialize-platform"))
    {
        Log.Information("Running platform initialization...");

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Step 1: Apply pending migrations
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied.");

        // Step 2: Seed required system roles
        var seeder = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Modules.Identity.Infrastructure.IdentitySeeder>();
        await seeder.SeedRolesAsync();
        Log.Information("System roles seeded.");

        // Step 3: Optional bootstrap SuperAdmin (only if explicitly enabled in config)
        if (!app.Environment.IsProduction() || app.Configuration.GetSection("Bootstrap").GetValue<bool>("Enabled"))
        {
            await seeder.SeedBootstrapSuperAdminAsync();
            Log.Information("Bootstrap SuperAdmin processed (if enabled).");
        }

        Log.Information("Platform initialization complete. Exiting.");
        return; // Exit without starting web server
    }

    // P2-4: ForwardedHeaders MUST run first in the pipeline.
    // This ensures subsequent middleware (CORS, auth, cookies) see the original
    // client scheme/host/IP when behind a reverse proxy.
    app.UseForwardedHeaders();

    // Database migration — Development/Test only.
    // Production: migrations must be applied via CI/CD or explicit deployment step.
    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "IntegrationTest" || app.Environment.EnvironmentName == "SecurityTest")
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    // P2-8: Role seeding — only in Development/test environments.
    // Production deployments should use an explicit initialization/migration step,
    // not rely on application startup to write seed data.
    // This prevents schema/deployment ownership blur in production.
    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "IntegrationTest" || app.Environment.EnvironmentName == "SecurityTest")
    {
        await app.Services.SeedIdentityRolesAsync();
    }

    // Bootstrap SuperAdmin — ONLY if explicitly enabled via config
    // and only in non-Production environments
    if (!app.Environment.IsProduction())
    {
        await app.Services.SeedBootstrapSuperAdminAsync();
    }

    // Middleware pipeline — CorrelationId BEFORE ExceptionHandling
    // so exception logs include the correlation context.
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    // P2-02: CSRF origin validation — runs after CORS, before auth.
    app.UseMiddleware<CsrfOriginMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();

    Log.Information("Application starting in {Environment}...", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible for WebApplicationFactory in tests
public partial class Program
{
}
