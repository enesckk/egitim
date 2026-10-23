using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using EgitimPlatform.Modules.Coaching.Features.ManageNotes;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Notebook.Features.ManageNotebook;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Features;
using EgitimPlatform.Modules.Students.Features.CreateStudent;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class NotebookAndCoachNotesIntegrationTests(IntegrationTestFactory factory)
{
    private async Task<HttpClient> GetAuthenticatedClient(string email, string password)
    {
        var client = factory.CreateClient();
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);
        return client;
    }

    [Fact]
    public async Task CoachPrivateNotes_FullLifecycle_ShouldSucceedForAssignedCoach()
    {
        // 1. Admin creates a student
        var adminClient = await GetAuthenticatedClient("admin-a@test.local", "Test@12345");
        var createStudentRes = await adminClient.PostAsJsonAsync("/api/v1/students", new CreateStudentCommand("Ali", "Kaya", null));
        createStudentRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var student = await createStudentRes.Content.ReadFromJsonAsync<StudentDto>();

        // 2. Assign coach A to this student
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var coachA = await dbContext.Set<Coach>().FirstAsync(c => c.FirstName == "Coach" && c.LastName == "A");

            var assignRes = await adminClient.PostAsJsonAsync("/api/v1/coaching/assign", new AssignCoachCommand(student!.Id, coachA.Id, true));
            assignRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // 3. Coach A creates a private note for this student
        var coachClient = await GetAuthenticatedClient("coach-a@test.local", "Test@12345");
        var createNoteReq = new CreateCoachPrivateNoteRequest(
            Category: "Akademik",
            Content: "Matematik denemelerinde zaman yönetimi çalışılacak.");

        var noteRes = await coachClient.PostAsJsonAsync($"/api/v1/coaching/students/{student!.Id}/notes", createNoteReq);
        noteRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdNote = await noteRes.Content.ReadFromJsonAsync<CoachPrivateNoteDto>();
        createdNote.Should().NotBeNull();
        createdNote!.Category.Should().Be("Akademik");
        createdNote.Content.Should().Be(createNoteReq.Content);

        // 4. Coach A lists notes
        var listRes = await coachClient.GetAsync($"/api/v1/coaching/students/{student.Id}/notes");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var notesList = await listRes.Content.ReadFromJsonAsync<List<CoachPrivateNoteDto>>();
        notesList.Should().Contain(n => n.Id == createdNote.Id);

        // 5. Coach A updates the note
        var updateNoteReq = new UpdateCoachPrivateNoteRequest(
            Category: "Motivasyon",
            Content: "Öğrenci zaman yönetimi hedefine ulaştı.");
        var updateRes = await coachClient.PutAsJsonAsync($"/api/v1/coaching/notes/{createdNote.Id}", updateNoteReq);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedNote = await updateRes.Content.ReadFromJsonAsync<CoachPrivateNoteDto>();
        updatedNote!.Category.Should().Be("Motivasyon");

        // 6. Coach A deletes the note
        var deleteRes = await coachClient.DeleteAsync($"/api/v1/coaching/notes/{createdNote.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. Verify soft delete
        var reListRes = await coachClient.GetAsync($"/api/v1/coaching/students/{student.Id}/notes");
        var reNotes = await reListRes.Content.ReadFromJsonAsync<List<CoachPrivateNoteDto>>();
        reNotes.Should().NotContain(n => n.Id == createdNote.Id);
    }

    [Fact]
    public async Task StudentNotebook_FullLifecycle_ShouldSucceedForStudent()
    {
        // 1. Provision a student user with student entity
        Guid studentUserId;
        Guid institutionId;
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var inst = await db.Set<EgitimPlatform.Modules.Institutions.Entities.Institution>().FirstAsync(i => i.Name == "Institution A");
            institutionId = inst.Id;

            var existing = await userManager.FindByEmailAsync("student-notebook-test@test.local");
            if (existing is null)
            {
                var user = new ApplicationUser
                {
                    UserName = "student-notebook-test@test.local",
                    Email = "student-notebook-test@test.local",
                    FirstName = "Defter",
                    LastName = "Öğrencisi",
                    InstitutionId = inst.Id,
                    IsActive = true,
                    EmailConfirmed = true,
                };
                await userManager.CreateAsync(user, "Test@12345");
                await userManager.AddToRoleAsync(user, Roles.Student);
                studentUserId = user.Id;

                var studentEntity = new Student
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    InstitutionId = inst.Id,
                    FirstName = "Defter",
                    LastName = "Öğrencisi",
                };
                db.Set<Student>().Add(studentEntity);
                await db.SaveChangesAsync();
            }
            else
            {
                studentUserId = existing.Id;
            }
        }

        // 2. Login as the student
        var studentClient = await GetAuthenticatedClient("student-notebook-test@test.local", "Test@12345");

        // 3. Create notebook entry
        var createCommand = new CreateNotebookEntryCommand(
            Title: "Fizik Newton Yasaları",
            Content: "1. Eylemsizlik, 2. F=m.a, 3. Etki-Tepki",
            Tags: "Fizik, AYT",
            IsPinned: false);

        var createRes = await studentClient.PostAsJsonAsync("/api/v1/notebook", createCommand);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdEntry = await createRes.Content.ReadFromJsonAsync<NotebookEntryDto>();
        createdEntry.Should().NotBeNull();
        createdEntry!.Title.Should().Be("Fizik Newton Yasaları");

        // 4. List notebook entries
        var listRes = await studentClient.GetAsync("/api/v1/notebook");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await listRes.Content.ReadFromJsonAsync<PaginatedList<NotebookEntryDto>>();
        page!.Items.Should().Contain(e => e.Id == createdEntry.Id);

        // 5. Update and pin notebook entry
        var updateReq = new UpdateNotebookEntryRequest(
            Title: "Fizik Newton Yasaları (Önemli)",
            Content: "1. Eylemsizlik, 2. F=m.a, 3. Etki-Tepki. Sürtünme Fs = k.N",
            Tags: "Fizik, AYT, Formül",
            IsPinned: true);

        var updateRes = await studentClient.PutAsJsonAsync($"/api/v1/notebook/{createdEntry.Id}", updateReq);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedEntry = await updateRes.Content.ReadFromJsonAsync<NotebookEntryDto>();
        updatedEntry!.IsPinned.Should().BeTrue();
        updatedEntry.Title.Should().Be("Fizik Newton Yasaları (Önemli)");

        // 6. Delete notebook entry
        var deleteRes = await studentClient.DeleteAsync($"/api/v1/notebook/{createdEntry.Id}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. Verify 404 on get
        var getRes = await studentClient.GetAsync($"/api/v1/notebook/{createdEntry.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
