using EgitimPlatform.Api.Extensions;
using EgitimPlatform.Api.Middleware;
using EgitimPlatform.Modules.Coaching.Extensions;
using EgitimPlatform.Modules.Identity.Extensions;
using EgitimPlatform.Modules.Students.Extensions;
using Serilog;

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

    // CORS
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

    var app = builder.Build();

    // Seed identity (roles + SuperAdmin)
    await app.Services.SeedIdentityAsync();

    // Middleware pipeline
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

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
    app.MapControllers();

    Log.Information("Application starting...");
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
