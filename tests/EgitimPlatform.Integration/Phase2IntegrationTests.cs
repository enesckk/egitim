using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Identity.Features.Users;
using EgitimPlatform.Modules.Students.Features;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Modules.Teachers.Features.ManageClassrooms;
using EgitimPlatform.Modules.Teachers.Features.ManageTeachers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Phase2IntegrationTests
{
    private readonly IntegrationTestFactory _factory;

    public Phase2IntegrationTests(IntegrationTestFactory factory)
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
    public async Task CreateUser_AsInstitutionAdmin_SuccessfullyCreatesUserWithRole()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");
        var uniqueEmail = $"new-student-{Guid.NewGuid():N}@test.local";
        var command = new CreateUserCommand(
            Email: uniqueEmail,
            Password: "Password123!",
            FirstName: "Ali",
            LastName: "Demir",
            Role: Roles.Student);

        var response = await client.PostAsJsonAsync("/api/v1/users", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(uniqueEmail);
        user.Role.Should().Be(Roles.Student);
        user.FirstName.Should().Be("Ali");
        user.LastName.Should().Be("Demir");
    }

    [Fact]
    public async Task CreateClassroom_AsInstitutionAdmin_CreatesAndRetrievesClassroom()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");
        var uniqueName = $"12-Fen-{Guid.NewGuid():N}"[..15];
        var command = new CreateClassroomCommand(
            InstitutionId: null,
            Name: uniqueName,
            Branch: "Kadıköy",
            GradeLevel: 12,
            AcademicYear: "2026-2027");

        var createResponse = await client.PostAsJsonAsync("/api/v1/classrooms", command);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ClassroomDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be(uniqueName);
        created.GradeLevel.Should().Be(12);

        var listResponse = await client.GetAsync($"/api/v1/classrooms?searchTerm={uniqueName}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<ClassroomDto>>();
        list.Should().NotBeNull();
        list.Should().ContainSingle(c => c.Id == created.Id);
    }

    [Fact]
    public async Task CreateClassroom_DuplicateNameSameInstitution_ReturnsConflict()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");
        var duplicateName = $"11-Dup-{Guid.NewGuid():N}"[..12];

        var command = new CreateClassroomCommand(
            InstitutionId: null,
            Name: duplicateName,
            Branch: "Merkez",
            GradeLevel: 11);

        var firstResponse = await client.PostAsJsonAsync("/api/v1/classrooms", command);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await client.PostAsJsonAsync("/api/v1/classrooms", command);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AssignTeacherAndStudent_ToClassroom_UpdatesClassroomDetailAndCounts()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");

        // 1. Create a Teacher user and domain entity
        var teacherEmail = $"teacher-{Guid.NewGuid():N}@test.local";
        var userRes = await client.PostAsJsonAsync("/api/v1/users", new CreateUserCommand(
            Email: teacherEmail,
            Password: "Password123!",
            FirstName: "Kemal",
            LastName: "Hoca",
            Role: Roles.Teacher));
        userRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var teacherUser = await userRes.Content.ReadFromJsonAsync<UserDto>();

        var teacherDomainRes = await client.PostAsJsonAsync("/api/v1/teachers", new CreateTeacherCommand(
            InstitutionId: teacherUser!.InstitutionId!.Value,
            UserId: teacherUser.Id,
            FirstName: "Kemal",
            LastName: "Hoca",
            Title: "Matematik"));
        teacherDomainRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var teacherDto = await teacherDomainRes.Content.ReadFromJsonAsync<TeacherDto>();

        // 2. Create a Student domain entity
        var studentRes = await client.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Murat", "Yıldız", null));
        studentRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var studentDto = await studentRes.Content.ReadFromJsonAsync<StudentDto>();

        // 3. Create Classroom
        var classRes = await client.PostAsJsonAsync("/api/v1/classrooms", new CreateClassroomCommand(
            InstitutionId: null,
            Name: $"Class-{Guid.NewGuid():N}"[..12],
            GradeLevel: 10));
        classRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var classroom = await classRes.Content.ReadFromJsonAsync<ClassroomDto>();

        // 4. Assign Teacher to Classroom
        var assignTeacherRes = await client.PostAsJsonAsync(
            $"/api/v1/classrooms/{classroom!.Id}/teachers",
            new AssignClassroomTeacherCommand(classroom.Id, teacherDto!.Id, IsPrimary: true));
        assignTeacherRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 5. Assign Student to Classroom
        var assignStudentRes = await client.PostAsJsonAsync(
            $"/api/v1/classrooms/{classroom.Id}/students",
            new AssignClassroomStudentCommand(classroom.Id, studentDto!.Id));
        assignStudentRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 6. Verify Classroom Detail
        var detailRes = await client.GetAsync($"/api/v1/classrooms/{classroom.Id}");
        detailRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await detailRes.Content.ReadFromJsonAsync<ClassroomDetailDto>();
        detail.Should().NotBeNull();
        detail!.Teachers.Should().ContainSingle(t => t.TeacherId == teacherDto.Id && t.IsPrimary);
        detail.Students.Should().ContainSingle(s => s.StudentId == studentDto.Id);

        // 7. Remove Teacher & Student
        var removeTeacherRes = await client.DeleteAsync($"/api/v1/classrooms/{classroom.Id}/teachers/{teacherDto.Id}");
        removeTeacherRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var removeStudentRes = await client.DeleteAsync($"/api/v1/classrooms/{classroom.Id}/students/{studentDto.Id}");
        removeStudentRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 8. Verify Removed
        var detailAfterRes = await client.GetAsync($"/api/v1/classrooms/{classroom.Id}");
        var detailAfter = await detailAfterRes.Content.ReadFromJsonAsync<ClassroomDetailDto>();
        detailAfter!.Teachers.Should().BeEmpty();
        detailAfter.Students.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateStudent_WithAtomicUserProvisioning_CreatesUserAndStudentTransactionally()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");
        var studentEmail = $"atomic-student-{Guid.NewGuid():N}@test.local";
        var password = "Password123!";

        var command = new CreateStudentCommand(
            FirstName: "Zeynep",
            LastName: "Kaya",
            Email: studentEmail,
            GradeLevel: 12,
            Password: password);

        var response = await client.PostAsJsonAsync("/api/v1/students", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var student = await response.Content.ReadFromJsonAsync<StudentDto>();
        student.Should().NotBeNull();
        student!.FirstName.Should().Be("Zeynep");
        student.LastName.Should().Be("Kaya");

        // Verify the created user can immediately authenticate
        var anonClient = _factory.CreateClient();
        var loginResponse = await anonClient.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(studentEmail, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        loginResult.Should().NotBeNull();
        loginResult!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateTeacher_WithAtomicUserProvisioning_CreatesUserAndTeacherTransactionally()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");
        var teacherEmail = $"atomic-teacher-{Guid.NewGuid():N}@test.local";
        var password = "Password123!";

        var command = new CreateTeacherCommand(
            InstitutionId: Guid.Empty,
            UserId: null,
            FirstName: "Selin",
            LastName: "Özkan",
            Title: "Biyoloji Öğretmeni",
            Email: teacherEmail,
            Password: password);

        var response = await client.PostAsJsonAsync("/api/v1/teachers", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var teacher = await response.Content.ReadFromJsonAsync<TeacherDto>();
        teacher.Should().NotBeNull();
        teacher!.FirstName.Should().Be("Selin");
        teacher.LastName.Should().Be("Özkan");

        // Verify teacher login
        var anonClient = _factory.CreateClient();
        var loginResponse = await anonClient.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(teacherEmail, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListExams_AsInstitutionAdmin_ReturnsPaginatedList()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");

        var response = await client.GetAsync("/api/v1/exams?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateStudent_WhenConflictOccurs_TrueTransactionRollsBackLeavingNoOrphanUser()
    {
        var client = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");

        // 1. Create initial student
        var uniqueLastName = $"RollbackTest-{Guid.NewGuid():N}"[..12];
        var firstCommand = new CreateStudentCommand(
            FirstName: "Ahmet",
            LastName: uniqueLastName,
            Email: null,
            GradeLevel: 11);
        var firstRes = await client.PostAsJsonAsync("/api/v1/students", firstCommand);
        firstRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Try to create duplicate student with same FirstName & LastName using atomic user provisioning
        var failedUserEmail = $"fail-orphan-{Guid.NewGuid():N}@test.local";
        var duplicateCommand = new CreateStudentCommand(
            FirstName: "Ahmet",
            LastName: uniqueLastName,
            Email: failedUserEmail,
            Password: "Password123!",
            GradeLevel: 11);

        var dupRes = await client.PostAsJsonAsync("/api/v1/students", duplicateCommand);
        dupRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 3. Verify no orphan ApplicationUser exists in database by attempting login
        var anonClient = _factory.CreateClient();
        var loginResponse = await anonClient.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(failedUserEmail, "Password123!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
