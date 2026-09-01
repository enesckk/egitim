using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Security.Fixtures;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class CrossInstitutionTests
{
    private readonly SecurityTestFactory _factory;

    public CrossInstitutionTests(SecurityTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> LoginAs(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "Test@12345"));
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            $"Login as {email} should succeed");
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Unauthenticated_GetStudents_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/students");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unauthenticated_CreateStudent_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Test", "User", null));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CoachA_CannotSeeStudentCreatedByAdminB()
    {
        // Admin B creates a student in Institution B
        var adminBClient = await LoginAs("sec-admin-b@test.local");
        var createResponse = await adminBClient.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("BStudent", "Test", null));
        var student = await createResponse.Content.ReadFromJsonAsync<EgitimPlatform.Modules.Students.Features.StudentDto>();

        // Coach A (Institution A) tries to access it
        var coachAClient = await LoginAs("sec-coach-a@test.local");
        var response = await coachAClient.GetAsync($"/api/v1/students/{student!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminA_CannotSeeStudentCreatedByAdminB()
    {
        // Admin B creates a student in Institution B
        var adminBClient = await LoginAs("sec-admin-b@test.local");
        var createResponse = await adminBClient.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("AnotherBStudent", "Test", null));
        var student = await createResponse.Content.ReadFromJsonAsync<EgitimPlatform.Modules.Students.Features.StudentDto>();

        // Admin A (Institution A) tries to access it
        var adminAClient = await LoginAs("sec-admin-a@test.local");
        var response = await adminAClient.GetAsync($"/api/v1/students/{student!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CoachA_CannotAssignCoachToStudentInInstitutionB()
    {
        // Admin B creates a student in Institution B
        var adminBClient = await LoginAs("sec-admin-b@test.local");
        var createResponse = await adminBClient.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("CrossInstStudent", "Test", null));
        var student = await createResponse.Content.ReadFromJsonAsync<EgitimPlatform.Modules.Students.Features.StudentDto>();

        // Coach A tries to assign themselves to this student
        var coachAClient = await LoginAs("sec-coach-a@test.local");
        var assignCommand = new EgitimPlatform.Modules.Coaching.Features.AssignCoach.AssignCoachCommand(student!.Id, SecurityTestDataSeeder.CoachAEntityId, true);
        var response = await coachAClient.PostAsJsonAsync("/api/v1/coaching/assign", assignCommand);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
