using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Security.Fixtures;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Security;

/// <summary>
/// Additional security tests covering hardened behaviors.
/// Note: Tests that modify shared state (lockout counters, etc.) are avoided
/// to prevent test-ordering issues in batch execution.
/// </summary>
[Collection("Security")]
public class CriticalSecurityTests
{
    private readonly SecurityTestFactory _factory;

    public CriticalSecurityTests(SecurityTestFactory factory)
    {
        _factory = factory;
    }

    // ===== Correlation ID Validation (P2-05) =====

    [Fact]
    public async Task CorrelationId_InvalidFormat_ReplacedWithServerGenerated()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "INVALID<>{}CORRELATION<>");
        var response = await client.GetAsync("/api/v1/students");

        // Should still respond (with 401 since no auth) but correlation ID should be valid
        response.Headers.Should().ContainKey("X-Correlation-Id");
        var returnedId = response.Headers.GetValues("X-Correlation-Id").First();
        returnedId.Should().NotBe("INVALID<>{}CORRELATION<>");
        returnedId.Length.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task CorrelationId_ValidGuid_Accepted()
    {
        var client = _factory.CreateClient();
        var correlationId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);
        var response = await client.GetAsync("/api/v1/students");

        var returnedId = response.Headers.GetValues("X-Correlation-Id").First();
        returnedId.Should().Be(correlationId);
    }

    // ===== Refresh Token (P1-03/04) =====

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/v1/auth/refresh", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===== Token Expiry (P3-01) =====

    [Fact]
    public async Task Login_ReturnsAccessTokenWithValidExpiry()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("sec-admin-a@test.local", "Test@12345"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        result!.AccessTokenExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        result.AccessTokenExpiresAt.Should().BeBefore(DateTimeOffset.UtcNow.AddHours(1));
    }

    // ===== ProblemDetails Format (P2-06) =====

    [Fact]
    public async Task Login_Failure_ReturnsProblemDetailsFormat()
    {
        var client = _factory.CreateClient();
        // Invalid credentials → controller returns ProblemDetails
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("nobody@test.local", "wrong"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("type");
        body.Should().Contain("title");
    }

    // ===== Lockout / No Enumeration (P1-07) =====

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand("nonexistent-user-xyz@test.local", "Wrong@123"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
