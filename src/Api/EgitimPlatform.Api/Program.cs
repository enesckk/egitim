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
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("CorsSettings:AllowedOrigins")
                .Get<string[]>();

            if (allowedOrigins is { Length: > 0 })
            {
                // P2-3: Credentialed browser requests (refresh cookie) require explicit origins.
                // WithOrigins + AllowCredentials is the correct combination.
                // Wildcard + credentials is forbidden by the CORS spec.
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else if (builder.Environment.IsDevelopment())
            {
                // Development convenience: permissive but without credentials
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // Production fallback: deny all if no origins configured
                // (fail-closed, not fail-open)
                policy.SetIsOriginAllowed(_ => false)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        });
    });

    // P2-4: ForwardedHeaders — must be configured BEFORE other middleware
    // so that Request.Scheme / Request.IsHttps reflect the original client values
    // when behind a reverse proxy (Plesk, Nginx, TLS termination, etc.).
    // Without this, Secure cookies would be dropped because Request.IsHttps = false behind proxy.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                 | ForwardedHeaders.XForwardedProto
                                 | ForwardedHeaders.XForwardedHost;
        options.ForwardLimit = null;

        // P2-4: Clear default known networks/proxies to trust all forwarded headers.
        // For production with specific proxy infrastructure, configure KnownProxies
        // or KnownIPNetworks via ForwardedHeaders:KnownNetworks in appsettings.
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
    });

    // OpenAPI
    builder.Services.AddEndpointsApiExplorer();

    // Rate limiting for auth endpoints
    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

        options.AddPolicy("refresh", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    var app = builder.Build();

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
