using System.Net;
using System.Net.Http.Json;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Questions.Constants;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Questions.Features.GetQuestionStyles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class SprintAQuestionStyleIntegrationTests(IntegrationTestFactory factory)
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(factory.ConnectionString).Options);

    private static string Code() => "SQS-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task QuestionStyles_SeededInMssql_ContainsAllEightControlledStylesInOrder()
    {
        using var db = Db();

        var styles = await db.Set<QuestionStyle>()
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();

        styles.Should().HaveCount(8);

        var expectedCodes = new[]
        {
            QuestionStyleDefaults.Codes.NewGeneration,
            QuestionStyleDefaults.Codes.Conceptual,
            QuestionStyleDefaults.Codes.Calculation,
            QuestionStyleDefaults.Codes.Interpretation,
            QuestionStyleDefaults.Codes.GraphTable,
            QuestionStyleDefaults.Codes.Problem,
            QuestionStyleDefaults.Codes.Visual,
            QuestionStyleDefaults.Codes.MultiStep
        };

        styles.Select(s => s.Code).Should().ContainInOrder((IEnumerable<string>)expectedCodes);
        styles.Should().OnlyContain(s => s.IsActive);
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync(string email = "admin-a@test.local", string password = "Test@12345")
    {
        var client = factory.CreateClient();
        var loginCommand = new EgitimPlatform.Modules.Identity.Features.Login.LoginCommand(email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<EgitimPlatform.Modules.Identity.Features.Login.LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);
        return client;
    }

    [Fact]
    public async Task GetQuestionStyles_ApiEndpoint_ReturnsOrderedActiveStyles_WhenAuthenticated()
    {
        var client = await GetAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/question-styles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<QuestionStyleDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(8);
        result!.Select(s => s.Code).Should().ContainInOrder(
            "NEW_GENERATION", "CONCEPTUAL", "CALCULATION", "INTERPRETATION",
            "GRAPH_TABLE", "PROBLEM", "VISUAL", "MULTI_STEP");
    }

    [Fact]
    public async Task GetQuestionStyles_ApiEndpoint_ReturnsUnauthorized_WhenAnonymous()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/question-styles");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task QuestionWithStyles_PersistsInMssql_AndCascadesOnDelete()
    {
        using var db = Db();

        var inst = new Institution { Name = "Inst Styles Test " + Guid.NewGuid().ToString("N")[..6] };
        db.Add(inst);

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Fizik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Kuvvet" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var styles = await db.Set<QuestionStyle>().Take(2).ToListAsync();
        styles.Should().HaveCount(2);

        var question = new Question
        {
            InstitutionId = inst.Id,
            SubjectId = subject.Id,
            TopicId = topic.Id,
            QuestionType = QuestionType.MultipleChoice,
            Difficulty = DifficultyLevel.Medium,
            SourceType = QuestionSourceType.InstitutionCustom,
            Content = "Test Soru Çoklu Tarz",
            Status = QuestionStatus.Draft
        };

        question.StyleAssignments.Add(new QuestionStyleAssignment
        {
            QuestionId = question.Id,
            QuestionStyleId = styles[0].Id
        });
        question.StyleAssignments.Add(new QuestionStyleAssignment
        {
            QuestionId = question.Id,
            QuestionStyleId = styles[1].Id
        });

        db.Add(question);
        await db.SaveChangesAsync();

        // Verify persisted in DB
        var assignments = await db.Set<QuestionStyleAssignment>()
            .Where(a => a.QuestionId == question.Id)
            .ToListAsync();
        assignments.Should().HaveCount(2);

        // Delete question -> should cascade delete junction rows
        db.Set<Question>().Remove(question);
        await db.SaveChangesAsync();

        var remainingAssignments = await db.Set<QuestionStyleAssignment>()
            .Where(a => a.QuestionId == question.Id)
            .ToListAsync();
        remainingAssignments.Should().BeEmpty();

        // Master styles must remain untouched
        var remainingStyles = await db.Set<QuestionStyle>().CountAsync();
        remainingStyles.Should().Be(8);
    }

    [Fact]
    public async Task QuestionStyle_DeleteRestrict_WhenAssignedToQuestion()
    {
        using var db = Db();

        var inst = new Institution { Name = "Inst Restrict Test " + Guid.NewGuid().ToString("N")[..6] };
        db.Add(inst);

        var examType = new ExamType { Code = Code(), Name = "AYT" };
        var subject = new Subject { Code = Code(), Name = "Kimya", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Gazlar" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var style = await db.Set<QuestionStyle>().FirstAsync();

        var question = new Question
        {
            InstitutionId = inst.Id,
            SubjectId = subject.Id,
            TopicId = topic.Id,
            QuestionType = QuestionType.MultipleChoice,
            Difficulty = DifficultyLevel.Hard,
            SourceType = QuestionSourceType.InstitutionCustom,
            Content = "Test Restrict",
            Status = QuestionStatus.Draft
        };

        question.StyleAssignments.Add(new QuestionStyleAssignment
        {
            QuestionId = question.Id,
            QuestionStyleId = style.Id
        });

        db.Add(question);
        await db.SaveChangesAsync();

        // Deleting the referenced style must throw DbUpdateException due to OnDelete(DeleteBehavior.Restrict) in MSSQL
        using var deleteDb = Db();
        var styleToDelete = await deleteDb.Set<QuestionStyle>().FindAsync(style.Id);
        styleToDelete.Should().NotBeNull();
        deleteDb.Set<QuestionStyle>().Remove(styleToDelete!);
        var act = () => deleteDb.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
