using System.Net;
using System.Text.Json;
using EgitimPlatform.Api.Health;
using EgitimPlatform.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EgitimPlatform.Unit;

public class HealthCheckUnitTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task DatabaseHealthCheck_WhenDatabaseIsReachable_ReturnsHealthy()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var serviceProvider = Substitute.For<IServiceProvider>();
        var serviceScope = Substitute.For<IServiceScope>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();

        serviceScope.ServiceProvider.GetService(typeof(ApplicationDbContext)).Returns(dbContext);
        serviceScopeFactory.CreateScope().Returns(serviceScope);
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);

        var check = new DatabaseHealthCheck(serviceProvider);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("database", check, null, null)
        };

        // Act
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Database is reachable.");
    }

    [Fact]
    public async Task DatabaseHealthCheck_WhenDatabaseThrowsException_ReturnsUnhealthyWithoutSecrets()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();

        // Service scope factory throws exception with sensitive connection string info
        serviceScopeFactory.CreateScope().Throws(new InvalidOperationException("Failed connecting to Server=secret-db.internal;User=admin;Password=secretPassword123!"));
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);

        var check = new DatabaseHealthCheck(serviceProvider);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("database", check, null, null)
        };

        // Act
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Database is unavailable.");
        result.Description.Should().NotContain("secretPassword123");
        result.Description.Should().NotContain("secret-db");
        result.Exception.Should().BeNull();
    }

    [Fact]
    public async Task DatabaseHealthCheck_WhenTimedOut_ReturnsUnhealthyTimeout()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var serviceScope = Substitute.For<IServiceScope>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();

        // Simulate cancellation / timeout
        serviceScopeFactory.When(x => x.CreateScope()).Do(_ => throw new OperationCanceledException());
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);

        var check = new DatabaseHealthCheck(serviceProvider);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("database", check, null, null)
        };

        // Act - with an uncancelled caller token so it simulates internal timeout
        var result = await check.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Database connectivity check timed out.");
    }

    [Fact]
    public async Task SanitizedHealthResponseWriter_Healthy_WritesMinimalJson()
    {
        // Arrange
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.FromMilliseconds(10));

        // Act
        await HealthCheckEndpointExtensions.WriteSanitizedHealthResponse(httpContext, report);

        // Assert
        httpContext.Response.ContentType.Should().Contain("application/json");
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var body = await reader.ReadToEndAsync();
        body.Should().Be("{\"status\":\"Healthy\"}");
    }

    [Fact]
    public async Task SanitizedHealthResponseWriter_Unhealthy_WritesMinimalJsonWithoutExceptionDetails()
    {
        // Arrange
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        var entry = new HealthReportEntry(
            HealthStatus.Unhealthy,
            description: "sensitive internal info",
            duration: TimeSpan.FromMilliseconds(10),
            exception: new Exception("Database connection string Server=10.0.0.1;Password=secret"),
            data: null);

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry> { ["database"] = entry },
            HealthStatus.Unhealthy,
            TimeSpan.FromMilliseconds(10));

        // Act
        await HealthCheckEndpointExtensions.WriteSanitizedHealthResponse(httpContext, report);

        // Assert
        httpContext.Response.ContentType.Should().Contain("application/json");
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var body = await reader.ReadToEndAsync();
        body.Should().Be("{\"status\":\"Unhealthy\"}");
        body.Should().NotContain("sensitive");
        body.Should().NotContain("secret");
    }
}
