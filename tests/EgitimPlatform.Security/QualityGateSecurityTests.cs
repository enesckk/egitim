using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Security.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Security;

/// <summary>
/// Quality-gate security tests covering all Codex re-review findings.
/// Tests verify actual security behavior, not just HTTP status codes.
/// </summary>
[Collection("Security")]
public class QualityGateSecurityTests
{
    private readonly SecurityTestFactory _factory;

    public QualityGateSecurityTests(SecurityTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> LoginAs(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "Test@12345"));
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login as {email} should succeed");
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.AccessToken);
        return client;
    }

    // ===== P2-3: Refresh token absent from JSON response =====

    [Fact]
    public async Task Login_RefreshTokenAbsentFromJson()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("sec-admin-a@test.local", "Test@12345"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        // RefreshToken field was removed from DTO — body should not contain a "refreshToken" property
        body.Should().NotContain("\"refreshToken\"");
    }

    [Fact]
    public async Task Login_RefreshTokenSetAsHttpOnlyCookie()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("sec-admin-a@test.local", "Test@12345"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.Should().ContainKey("Set-Cookie");
        var setCookie = response.Headers.GetValues("Set-Cookie").First();
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("samesite=strict");
        setCookie.Should().Contain("path=/api/v1/auth");
    }

    // ===== P1-01: Coach ID Mapping — UserId ≠ Coach.Id =====

    [Fact]
    public async Task Coach_UserId_DiffersFrom_CoachId_AssignedAccessSucceeds()
    {
        // This test verifies the P1-01 fix: the authorization flow correctly maps
        // ApplicationUser.Id → Coach.Id → StudentCoachAssignment, even when these IDs differ.

        // Admin creates a student
        var adminClient = await LoginAs("sec-admin-a@test.local");
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/students",
            new CreateStudentCommand("P1Test", "Student", null));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var student = await createResponse.Content.ReadFromJsonAsync<EgitimPlatform.Modules.Students.Features.StudentDto>();

        // Admin assigns the coach to the student
        var assignCmd = new EgitimPlatform.Modules.Coaching.Features.AssignCoach.AssignCoachCommand(
            student!.Id, SecurityTestDataSeeder.CoachAEntityId, true);
        var assignResponse = await adminClient.PostAsJsonAsync("/api/v1/coaching/assign", assignCmd);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Coach can now access the student (even though Coach.UserId ≠ Coach.Id)
        var coachClient = await LoginAs("sec-coach-a@test.local");
        var getResponse = await coachClient.GetAsync($"/api/v1/students/{student.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Assigned coach should access student regardless of UserId vs Coach.Id difference");
    }

    [Fact]
    public async Task Coach_ListOnlyShowsAssignedStudents()
    {
        // Admin creates two students
        var adminClient = await LoginAs("sec-admin-a@test.local");
        var r1 = await adminClient.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Assigned", "One", null));
        var s1 = await r1.Content.ReadFromJsonAsync<EgitimPlatform.Modules.Students.Features.StudentDto>();
        var r2 = await adminClient.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Unassigned", "Two", null));

        // Admin assigns coach to first student only
        var assignCmd = new EgitimPlatform.Modules.Coaching.Features.AssignCoach.AssignCoachCommand(
            s1!.Id, SecurityTestDataSeeder.CoachAEntityId, true);
        await adminClient.PostAsJsonAsync("/api/v1/coaching/assign", assignCmd);

        // Coach lists students — should see only the assigned one
        var coachClient = await LoginAs("sec-coach-a@test.local");
        var listResponse = await coachClient.GetAsync("/api/v1/students");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await listResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Assigned");
        body.Should().NotContain("Unassigned");
    }

    [Fact]
    public async Task UnassignedCoach_SameInstitution_DeniedAccess()
    {
        // Admin creates a student but does NOT assign Coach B
        var adminClient = await LoginAs("sec-admin-a@test.local");
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/students",
            new CreateStudentCommand("NoAssign", "Test", null));
        var student = await createResponse.Content.ReadFromJsonAsync<EgitimPlatform.Modules.Students.Features.StudentDto>();

        // Coach A (same institution, but NOT assigned) tries to access
        var coachClient = await LoginAs("sec-coach-a@test.local");
        var getResponse = await coachClient.GetAsync($"/api/v1/students/{student!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Unassigned coach in same institution must be denied");
    }

    // ===== P2-10: Multiple Correlation ID values rejected =====

    [Fact]
    public async Task MultipleCorrelationIdHeaders_ReplacedWithServerGenerated()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", new[] { "valid-one", "valid-two" });
        var response = await client.GetAsync("/api/v1/students");

        var returnedId = response.Headers.GetValues("X-Correlation-Id").First();
        returnedId.Should().NotBe("valid-one");
        returnedId.Should().NotBe("valid-two");
    }

    // ===== P2-11: ProblemDetails for validation errors =====

    [Fact]
    public async Task CoachingValidation_ReturnsProblemDetails()
    {
        var client = await LoginAs("sec-admin-a@test.local");
        // Invalid command (empty GUIDs)
        var response = await client.PostAsJsonAsync("/api/v1/coaching/assign",
            new { StudentId = Guid.Empty, CoachId = Guid.Empty, IsPrimary = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("type");
        body.Should().Contain("title");
    }

    // ===== P2-7: Bootstrap audit =====

    [Fact]
    public async Task BootstrapSuperAdmin_CreatesAuditLog()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EgitimPlatform.Infrastructure.ApplicationDbContext>();

        var bootstrapAudit = await context.AuditLogs
            .AnyAsync(a => a.Action == "Auth.Bootstrap.SuperAdmin.Created");

        bootstrapAudit.Should().BeTrue("Bootstrap should create an immutable audit log entry");
    }

    // ===== Cross-tenant DB invariant (P2-6) =====

    [Fact]
    public async Task CrossInstitution_Assignment_DeniedAtApplicationLevel()
    {
        // Admin B creates student in Institution B
        var adminBClient = await LoginAs("sec-admin-b@test.local");
        var createResponse = await adminBClient.PostAsJsonAsync("/api/v1/students",
            new CreateStudentCommand("CrossTenantP26", "Test", null));
        var student = await createResponse.Content.ReadFromJsonAsync<EgitimPlatform.Modules.Students.Features.StudentDto>();

        // Coach A (Institution A) tries to assign — denied by application-level check
        var coachAClient = await LoginAs("sec-coach-a@test.local");
        var assignCmd = new EgitimPlatform.Modules.Coaching.Features.AssignCoach.AssignCoachCommand(
            student!.Id, SecurityTestDataSeeder.CoachAEntityId, true);
        var response = await coachAClient.PostAsJsonAsync("/api/v1/coaching/assign", assignCmd);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
