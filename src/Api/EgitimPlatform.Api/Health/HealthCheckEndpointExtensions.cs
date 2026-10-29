using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EgitimPlatform.Api.Health;

public static class HealthCheckEndpointExtensions
{
    private static readonly byte[] HealthyBytes = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"Healthy\"}");
    private static readonly byte[] DegradedBytes = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"Degraded\"}");
    private static readonly byte[] UnhealthyBytes = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"Unhealthy\"}");

    public static Task WriteSanitizedHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var bytes = report.Status switch
        {
            HealthStatus.Healthy => HealthyBytes,
            HealthStatus.Degraded => DegradedBytes,
            _ => UnhealthyBytes
        };

        return context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
    }

    public static async Task<int> RunCliHealthProbeAsync(string[] args)
    {
        var path = args.Contains("--check-health-live") ? "/health/live" : "/health/ready";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
        try
        {
            using var response = await client.GetAsync($"http://127.0.0.1:5005{path}");
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch
        {
            return 1;
        }
    }
}
