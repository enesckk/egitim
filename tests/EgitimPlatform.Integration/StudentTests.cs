using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Integration.TestData;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Students.Features;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Modules.Students.Features.UpdateAcademicProfile;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class StudentTests
{
    private readonly IntegrationTestFactory _factory;

    public StudentTests(IntegrationTestFactory factory)
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
    public async Task CreateStudent_AsInstitutionAdmin_ReturnsCreated()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");
        var command = new CreateStudentCommand("John", "Doe", null);

        var response = await client.PostAsJsonAsync("/api/v1/students", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var student = await response.Content.ReadFromJsonAsync<StudentDto>();
        student.Should().NotBeNull();
        student!.FirstName.Should().Be("John");
        student.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task GetStudent_AsInstitutionAdmin_ReturnsStudent()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");

        // Create student first
        var createCommand = new CreateStudentCommand("Jane", "Smith", null);
        var createResponse = await client.PostAsJsonAsync("/api/v1/students", createCommand);
        var created = await createResponse.Content.ReadFromJsonAsync<StudentDto>();

        // Get student
        var response = await client.GetAsync($"/api/v1/students/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var student = await response.Content.ReadFromJsonAsync<StudentDto>();
        student!.FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task ListStudents_AsInstitutionAdmin_ReturnsPaginatedWithAcademicProfileFields()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");

        // Create a student and update academic profile
        var createRes = await client.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Alice", "ProfileTest", null));
        var created = await createRes.Content.ReadFromJsonAsync<StudentDto>();

        var uniqueNumber = $"SN-{Guid.NewGuid().ToString("N")[..8]}";
        var profileCmd = new UpdateAcademicProfileCommand(created!.Id, "Fen Lisesi", uniqueNumber, 12, DateTimeOffset.UtcNow);
        var updateRes = await client.PutAsJsonAsync($"/api/v1/students/{created.Id}/academic-profile", profileCmd);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.GetAsync("/api/v1/students?page=1&pageSize=50&searchTerm=ProfileTest");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PaginatedList<StudentDto>>();
        page.Should().NotBeNull();
        var item = page!.Items.Should().ContainSingle(s => s.Id == created.Id).Subject;
        item.SchoolName.Should().Be("Fen Lisesi");
        item.GradeLevel.Should().Be(12);
        item.StudentNumber.Should().Be(uniqueNumber);
    }

    [Fact]
    public async Task CreateStudent_WithEmptyFirstName_ReturnsBadRequest()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");
        var command = new CreateStudentCommand("", "Doe", null);

        var response = await client.PostAsJsonAsync("/api/v1/students", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
