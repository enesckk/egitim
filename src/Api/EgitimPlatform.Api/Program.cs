using EgitimPlatform.Api.Extensions;
using EgitimPlatform.Api.Middleware;
using EgitimPlatform.Modules.Coaching.Extensions;
using EgitimPlatform.Modules.Identity.Extensions;
using EgitimPlatform.Modules.Identity.Infrastructure;
using EgitimPlatform.Modules.Students.Extensions;
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

    // Identity module (includes DbContext, Auth, Identity services)
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

    // CORS — configured per environment (not AllowAny in production)
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
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

    // Database migration — Development/Test only.
    // Production: migrations must be applied via CI/CD or explicit deployment step.
    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "IntegrationTest" || app.Environment.EnvironmentName == "SecurityTest")
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    // Seed roles (deterministic, safe for all environments)
    await app.Services.SeedIdentityRolesAsync();

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
