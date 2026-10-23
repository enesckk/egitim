using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using EgitimPlatform.Modules.Coaching.Features.ManageNotes;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Notebook.Features.ManageNotebook;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Features;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using EgitimPlatform.Security.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Security;

[Collection("Security")]
public class NotebookAndCoachNotesSecurityTests(SecurityTestFactory factory)
{
    private async Task<HttpClient> LoginAs(string email, string password = "Test@12345")
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login as {email} should succeed");
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.AccessToken);
        return client;
    }

    [Fact]
    public async Task CoachPrivateNotes_InstitutionAdmin_ReturnsForbidden()
    {
        // InstitutionAdmin must NEVER be able to read or write coach private notes
        var adminClient = await LoginAs("sec-admin-a@test.local");
        var studentId = Guid.NewGuid();

        var getRes = await adminClient.GetAsync($"/api/v1/coaching/students/{studentId}/notes");
        getRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "InstitutionAdmin must not access coach private notes");

        var createReq = new CreateCoachPrivateNoteRequest("Akademik", "Admin notu");
        var postRes = await adminClient.PostAsJsonAsync($"/api/v1/coaching/students/{studentId}/notes", createReq);
        postRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "InstitutionAdmin must not create coach private notes");
    }

    [Fact]
    public async Task CoachPrivateNotes_UnassignedCoach_ReturnsForbidden()
    {
        // 1. Create a student in Institution A and assign Coach A
        var adminClient = await LoginAs("sec-admin-a@test.local");
        var createStudentRes = await adminClient.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Can", "Yılmaz", null));
        createStudentRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var student = await createStudentRes.Content.ReadFromJsonAsync<StudentDto>();

        // 2. Coach B (from Institution B or unassigned) attempts to access notes for this student
        var coachBClient = await LoginAs("sec-coach-b@test.local");

        var getRes = await coachBClient.GetAsync($"/api/v1/coaching/students/{student!.Id}/notes");
        getRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Unassigned coach must not read student's coach notes");

        var createReq = new CreateCoachPrivateNoteRequest("Genel", "İzinsiz erişim denemesi");
        var postRes = await coachBClient.PostAsJsonAsync($"/api/v1/coaching/students/{student.Id}/notes", createReq);
        postRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Unassigned coach must not create student's coach notes");
    }

    [Fact]
    public async Task StudentNotebook_NonStudentRoles_ReturnsForbidden()
    {
        // Neither InstitutionAdmin nor Coach should be able to access the student's personal notebook
        var adminClient = await LoginAs("sec-admin-a@test.local");
        var adminListRes = await adminClient.GetAsync("/api/v1/notebook");
        adminListRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "InstitutionAdmin must not access student notebook");

        var coachClient = await LoginAs("sec-coach-a@test.local");
        var coachListRes = await coachClient.GetAsync("/api/v1/notebook");
        coachListRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Coach must not access student notebook");
    }

    [Fact]
    public async Task CoachPrivateNotes_RevokedAssignment_DeniesAllAccess()
    {
        // 1. Create a student in Institution A and assign Coach A
        var adminClient = await LoginAs("sec-admin-a@test.local");
        var createStudentRes = await adminClient.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Banu", "Demir", null));
        createStudentRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var student = await createStudentRes.Content.ReadFromJsonAsync<StudentDto>();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var coachA = await db.Set<Coach>().FirstAsync(c => c.FirstName == "CoachA");
            db.Set<StudentCoachAssignment>().Add(new StudentCoachAssignment
            {
                Id = Guid.NewGuid(),
                InstitutionId = SecurityTestDataSeeder.InstitutionAId,
                StudentId = student!.Id,
                CoachId = coachA.Id,
                IsPrimary = true,
                IsActive = true,
                AssignedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        // 2. Active Coach A creates a private note
        var coachAClient = await LoginAs("sec-coach-a@test.local");
        var createReq = new CreateCoachPrivateNoteRequest("Akademik", "Aktifken eklenen not");
        var createRes = await coachAClient.PostAsJsonAsync($"/api/v1/coaching/students/{student!.Id}/notes", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdNote = await createRes.Content.ReadFromJsonAsync<CoachPrivateNoteDto>();

        // 3. Assignment is REVOKED (ended)
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var assignment = await db.Set<StudentCoachAssignment>().FirstAsync(a => a.StudentId == student.Id);
            assignment.IsActive = false;
            assignment.EndedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        // 4. Revoked Coach A attempts: List, Create, Update, Delete -> ALL MUST BE 403 FORBIDDEN
        var listRes = await coachAClient.GetAsync($"/api/v1/coaching/students/{student.Id}/notes");
        listRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Revoked coach cannot list private notes");

        var postRes = await coachAClient.PostAsJsonAsync($"/api/v1/coaching/students/{student.Id}/notes", new CreateCoachPrivateNoteRequest("Genel", "Yeni not"));
        postRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Revoked coach cannot create private notes");

        var putRes = await coachAClient.PutAsJsonAsync($"/api/v1/coaching/notes/{createdNote!.Id}", new UpdateCoachPrivateNoteRequest("Genel", "Güncelleme"));
        putRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Revoked coach cannot update private notes");

        var deleteRes = await coachAClient.DeleteAsync($"/api/v1/coaching/notes/{createdNote.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Revoked coach cannot delete private notes");
    }
}
