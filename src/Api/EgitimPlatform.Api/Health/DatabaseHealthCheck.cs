using EgitimPlatform.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EgitimPlatform.Api.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    public DatabaseHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(DefaultTimeout);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
            if (canConnect)
            {
                return HealthCheckResult.Healthy("Database is reachable.");
            }

            return HealthCheckResult.Unhealthy("Database is unreachable.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database connectivity check timed out.");
        }
        catch (Exception)
        {
            // Do not disclose internal exception messages, connection strings or stack traces
            return HealthCheckResult.Unhealthy("Database is unavailable.");
        }
    }
}
