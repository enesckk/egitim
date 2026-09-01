using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Integration.TestData;
using EgitimPlatform.Modules.Identity.Features.Login;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class AuthTests
{
    private readonly IntegrationTestFactory _factory;

    public AuthTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var command = new LoginCommand("admin-a@test.local", "Test@12345");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var command = new LoginCommand("admin-a@test.local", "WrongPassword");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var command = new LoginCommand("nobody@test.local", "Test@12345");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithSuperAdmin_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var command = new LoginCommand("superadmin@egitimplatform.local", "SuperAdmin@123!");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
