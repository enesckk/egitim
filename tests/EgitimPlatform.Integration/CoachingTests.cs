using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Integration.TestData;
using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class CoachingTests
{
    private readonly IntegrationTestFactory _factory;

    public CoachingTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> GetAuthenticatedClient(string email, string password)
    {
        var client = _factory.CreateClient();
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);
        return client;
    }

    [Fact]
    public async Task AssignCoach_AsInstitutionAdmin_ReturnsNoContent()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");

        // Create a student first
        var createResponse = await client.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Test", "Student", null));
        var student = await createResponse.Content.ReadFromJsonAsync<EgitimPlatform.Modules.Students.Features.StudentDto>();

        // Get coach entity ID from the database
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Modules.Identity.Infrastructure.ApplicationDbContext>();
        var coach = await dbContext.Set<EgitimPlatform.Modules.Coaching.Entities.Coach>()
            .FirstAsync(c => c.FirstName == "Coach" && c.LastName == "A");

        var command = new AssignCoachCommand(student!.Id, coach.Id, true);
        var response = await client.PostAsJsonAsync("/api/v1/coaching/assign", command);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
