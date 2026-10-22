using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Identity.Features.Users;
using EgitimPlatform.Modules.Students.Features;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Modules.Teachers.Features.ManageClassrooms;
using EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
using EgitimPlatform.Security.Fixtures;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class Phase2SecurityTests
{
    private readonly SecurityTestFactory _factory;

    public Phase2SecurityTests(SecurityTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> LoginAs(string email, string password = "Test@12345")
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login as {email} should succeed");
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.AccessToken);
        return client;
    }

    [Fact]
    public async Task CreateUser_SuperAdminRole_ReturnsForbidden()
    {
        var client = await LoginAs("sec-admin-a@test.local");
        var command = new CreateUserCommand(
            Email: $"hacker-{Guid.NewGuid():N}@test.local",
            Password: "Password123!",
            FirstName: "Privilege",
            LastName: "Escalator",
            Role: Roles.SuperAdmin);

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_InstitutionAdminRole_ReturnsForbidden()
    {
        var client = await LoginAs("sec-admin-a@test.local");
        var command = new CreateUserCommand(
            Email: $"fake-admin-{Guid.NewGuid():N}@test.local",
            Password: "Password123!",
            FirstName: "Sub",
            LastName: "Admin",
            Role: Roles.InstitutionAdmin);

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_CrossInstitution_ReturnsForbidden()
    {
        var client = await LoginAs("sec-admin-a@test.local");
        var command = new CreateUserCommand(
            Email: $"cross-user-{Guid.NewGuid():N}@test.local",
            Password: "Password123!",
            FirstName: "Cross",
            LastName: "User",
            Role: Roles.Student,
            InstitutionId: SecurityTestDataSeeder.InstitutionBId);

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_AsCoach_ReturnsForbidden()
    {
        var client = await LoginAs("sec-coach-a@test.local");
        var command = new CreateUserCommand(
            Email: $"coach-created-{Guid.NewGuid():N}@test.local",
            Password: "Password123!",
            FirstName: "Coach",
            LastName: "Student",
            Role: Roles.Student);

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Classroom_CrossInstitution_CannotAssignTeacherFromOtherInstitution()
    {
        var adminA = await LoginAs("sec-admin-a@test.local");
        var adminB = await LoginAs("sec-admin-b@test.local");

        // 1. Create a teacher in Institution B
        var teacherBEmail = $"t-inst-b-{Guid.NewGuid():N}@test.local";
        var userBRes = await adminB.PostAsJsonAsync("/api/v1/users", new CreateUserCommand(
            Email: teacherBEmail,
            Password: "Password123!",
            FirstName: "Teacher",
            LastName: "B",
            Role: Roles.Teacher));
        userBRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var teacherBUser = await userBRes.Content.ReadFromJsonAsync<UserDto>();

        var teacherBDomainRes = await adminB.PostAsJsonAsync("/api/v1/teachers", new CreateTeacherCommand(
            InstitutionId: teacherBUser!.InstitutionId!.Value,
            UserId: teacherBUser.Id,
            FirstName: "Teacher",
            LastName: "B"));
        teacherBDomainRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var teacherBDto = await teacherBDomainRes.Content.ReadFromJsonAsync<TeacherDto>();

        // 2. Admin A creates a classroom in Institution A
        var classRes = await adminA.PostAsJsonAsync("/api/v1/classrooms", new CreateClassroomCommand(
            InstitutionId: null,
            Name: $"Class-A-{Guid.NewGuid():N}"[..12],
            GradeLevel: 11));
        classRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var classroomA = await classRes.Content.ReadFromJsonAsync<ClassroomDto>();

        // 3. Admin A tries to assign Teacher B (from Institution B) to Classroom A -> must fail closed (NotFound)
        var assignRes = await adminA.PostAsJsonAsync(
            $"/api/v1/classrooms/{classroomA!.Id}/teachers",
            new AssignClassroomTeacherCommand(classroomA.Id, teacherBDto!.Id));
        assignRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Classroom_CrossInstitution_CannotAssignStudentFromOtherInstitution()
    {
        var adminA = await LoginAs("sec-admin-a@test.local");
        var adminB = await LoginAs("sec-admin-b@test.local");

        // 1. Admin B creates student in Institution B
        var studentBRes = await adminB.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Student", "InstB", null));
        studentBRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var studentBDto = await studentBRes.Content.ReadFromJsonAsync<StudentDto>();

        // 2. Admin A creates classroom in Institution A
        var classRes = await adminA.PostAsJsonAsync("/api/v1/classrooms", new CreateClassroomCommand(
            InstitutionId: null,
            Name: $"Class-Sec-{Guid.NewGuid():N}"[..12],
            GradeLevel: 10));
        classRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var classroomA = await classRes.Content.ReadFromJsonAsync<ClassroomDto>();

        // 3. Admin A tries to assign Student B to Classroom A -> must fail closed (NotFound)
        var assignRes = await adminA.PostAsJsonAsync(
            $"/api/v1/classrooms/{classroomA!.Id}/students",
            new AssignClassroomStudentCommand(classroomA.Id, studentBDto!.Id));
        assignRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TeacherStudentAccess_OnlyAssignedTeacherCanAccess360()
    {
        var adminA = await LoginAs("sec-admin-a@test.local");

        // 1. Create Teacher in Inst A
        var teacherEmail = $"assigned-t-{Guid.NewGuid():N}@test.local";
        var userRes = await adminA.PostAsJsonAsync("/api/v1/users", new CreateUserCommand(
            Email: teacherEmail,
            Password: "Password123!",
            FirstName: "Assigned",
            LastName: "Teacher",
            Role: Roles.Teacher));
        userRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var teacherUser = await userRes.Content.ReadFromJsonAsync<UserDto>();

        var teacherDomainRes = await adminA.PostAsJsonAsync("/api/v1/teachers", new CreateTeacherCommand(
            InstitutionId: teacherUser!.InstitutionId!.Value,
            UserId: teacherUser.Id,
            FirstName: "Assigned",
            LastName: "Teacher"));
        teacherDomainRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var teacherDto = await teacherDomainRes.Content.ReadFromJsonAsync<TeacherDto>();

        // 2. Create Student 1 (to be assigned) and Student 2 (unassigned)
        var student1Res = await adminA.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Assigned", "Student", null));
        var student1 = await student1Res.Content.ReadFromJsonAsync<StudentDto>();

        var student2Res = await adminA.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Unassigned", "Student", null));
        var student2 = await student2Res.Content.ReadFromJsonAsync<StudentDto>();

        // 3. Create Classroom and assign Teacher + Student 1
        var classRes = await adminA.PostAsJsonAsync("/api/v1/classrooms", new CreateClassroomCommand(
            InstitutionId: null,
            Name: $"Class-360-{Guid.NewGuid():N}"[..12],
            GradeLevel: 12));
        var classroom = await classRes.Content.ReadFromJsonAsync<ClassroomDto>();

        await adminA.PostAsJsonAsync(
            $"/api/v1/classrooms/{classroom!.Id}/teachers",
            new AssignClassroomTeacherCommand(classroom.Id, teacherDto!.Id, IsPrimary: true));

        await adminA.PostAsJsonAsync(
            $"/api/v1/classrooms/{classroom.Id}/students",
            new AssignClassroomStudentCommand(classroom.Id, student1!.Id));

        // 4. Log in as Teacher
        var teacherClient = await LoginAs(teacherEmail, "Password123!");

        // 5. Access Assigned Student 360 -> Must be 200 OK
        var assignedRes = await teacherClient.GetAsync($"/api/v1/students/{student1.Id}/360");
        assignedRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Access Unassigned Student 360 -> Must be 403 Forbidden
        var unassignedRes = await teacherClient.GetAsync($"/api/v1/students/{student2!.Id}/360");
        unassignedRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateStudent_WithWeakPassword_FailsValidationWithoutOrphanAccount()
    {
        var adminA = await LoginAs("sec-admin-a@test.local");
        var testEmail = $"weak-pass-{Guid.NewGuid():N}@test.local";

        var command = new CreateStudentCommand(
            FirstName: "Test",
            LastName: "Student",
            Email: testEmail,
            GradeLevel: 12,
            Password: "123"); // Weak password (fails ASP.NET Identity password requirements)

        var response = await adminA.PostAsJsonAsync("/api/v1/students", command);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);

        // Verify no orphan account exists that can be logged into
        var anonClient = _factory.CreateClient();
        var loginResponse = await anonClient.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(testEmail, "123"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
