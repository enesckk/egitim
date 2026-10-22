using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Planning.Features;
using EgitimPlatform.Modules.Questions.Constants;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class SprintCQuestionStyleIntegrationTests(IntegrationTestFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(factory.ConnectionString).Options);

    private static string Code() => "SQC-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private async Task<HttpClient> GetAuthenticatedAdminClientAsync()
    {
        var client = factory.CreateClient();
        var loginCommand = new LoginCommand("admin-a@test.local", "Test@12345");
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);
        return client;
    }

    [Fact]
    public async Task SprintC_EndToEnd_RealMssql_Recommendations_StudyPlan_AcceptTaskPersistence_AndAiSummary()
    {
        using var db = Db();

        var adminUser = await db.Set<EgitimPlatform.Modules.Identity.Entities.ApplicationUser>()
            .FirstAsync(u => u.Email == "admin-a@test.local");
        var instId = adminUser.InstitutionId!.Value;

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Problemler" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var student = new Student
        {
            InstitutionId = instId,
            UserId = Guid.NewGuid(),
            FirstName = "SprintC",
            LastName = "Student"
        };
        db.Add(student);
        await db.SaveChangesAsync();

        var styles = await db.Set<QuestionStyle>().OrderBy(s => s.DisplayOrder).ToListAsync();
        var styleNewGen = styles.First(s => s.Code == QuestionStyleDefaults.Codes.NewGeneration);
        var styleConcept = styles.First(s => s.Code == QuestionStyleDefaults.Codes.Conceptual);
        var styleGraph = styles.First(s => s.Code == QuestionStyleDefaults.Codes.GraphTable);

        // Create 3 multi-tagged questions (NEW_GENERATION) -> all answered wrong across 2 attempts (6 questions, 0 correct -> Critical)
        // Create 3 questions (CONCEPT_KNOWLEDGE) -> all answered right across 2 attempts (6 questions, 6 correct -> Strong)
        // Create 1 question (GRAPH_TABLE) -> answered wrong across 2 attempts (2 questions < 5 -> InsufficientData)
        var questions = new List<Question>();
        for (int i = 1; i <= 3; i++)
        {
            var qCrit = new Question { InstitutionId = instId, SubjectId = subject.Id, TopicId = topic.Id, Content = $"Crit Q{i}", Status = QuestionStatus.Published };
            qCrit.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = qCrit.Id, QuestionStyleId = styleNewGen.Id });
            questions.Add(qCrit);
        }
        for (int i = 1; i <= 3; i++)
        {
            var qStrong = new Question { InstitutionId = instId, SubjectId = subject.Id, TopicId = topic.Id, Content = $"Strong Q{i}", Status = QuestionStatus.Published };
            qStrong.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = qStrong.Id, QuestionStyleId = styleConcept.Id });
            questions.Add(qStrong);
        }
        var qInsuff = new Question { InstitutionId = instId, SubjectId = subject.Id, TopicId = topic.Id, Content = "Insuff Q", Status = QuestionStatus.Published };
        qInsuff.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = qInsuff.Id, QuestionStyleId = styleGraph.Id });
        questions.Add(qInsuff);

        db.AddRange(questions);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = instId, ExamTypeId = examType.Id, Code = Code(), Title = "Sprint C Deneme" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = instId, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 7 };
        db.Add(section);
        await db.SaveChangesAsync();

        var examQuestions = new List<ExamQuestion>();
        for (int idx = 0; idx < questions.Count; idx++)
        {
            var eq = new ExamQuestion
            {
                InstitutionId = instId,
                ExamId = exam.Id,
                ExamSectionId = section.Id,
                QuestionId = questions[idx].Id,
                QuestionNumber = idx + 1,
                Order = idx + 1,
                CorrectOption = "A"
            };
            examQuestions.Add(eq);
        }
        db.AddRange(examQuestions);
        await db.SaveChangesAsync();

        for (int attemptNum = 1; attemptNum <= 2; attemptNum++)
        {
            var attempt = new StudentExamAttempt
            {
                InstitutionId = instId,
                ExamId = exam.Id,
                StudentId = student.Id,
                Status = AttemptStatus.Finalized,
                AttemptNumber = attemptNum,
                IsRawAvailable = true,
                TakenAt = DateTimeOffset.UtcNow.AddDays(-4 + attemptNum)
            };
            db.Add(attempt);
            await db.SaveChangesAsync();

            for (int idx = 0; idx < questions.Count; idx++)
            {
                bool isCorrect = idx >= 3 && idx <= 5; // indices 3..5 are Strong (CONCEPT_KNOWLEDGE), 0..2 and 6 are Wrong
                db.Add(new StudentExamQuestionResult
                {
                    InstitutionId = instId,
                    StudentExamAttemptId = attempt.Id,
                    ExamQuestionId = examQuestions[idx].Id,
                    QuestionId = questions[idx].Id,
                    SelectedOption = isCorrect ? "A" : "B",
                    IsCorrect = isCorrect,
                    IsBlank = false,
                    NetContribution = isCorrect ? 1m : -0.25m
                });
            }

            db.Add(new StudentExamSubjectResult { InstitutionId = instId, StudentExamAttemptId = attempt.Id, ExamSectionId = section.Id, Correct = 3, Wrong = 4, Blank = 0, Net = 2m });
            db.Add(new StudentExamTopicResult { InstitutionId = instId, StudentExamAttemptId = attempt.Id, ExamSectionId = section.Id, TopicId = topic.Id, Correct = 3, Wrong = 4, Blank = 0, Net = 2m });
            await db.SaveChangesAsync();
        }

        using var client = await GetAuthenticatedAdminClientAsync();

        // 1. Verify /recommendations endpoint includes NEW_GENERATION (Critical) and excludes CONCEPT_KNOWLEDGE (Strong) and GRAPH_TABLE (InsufficientData)
        var recResp = await client.GetAsync($"/api/v1/students/{student.Id}/academic-intelligence/recommendations");
        recResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var recommendations = await recResp.Content.ReadFromJsonAsync<List<AcademicRecommendationDto>>(JsonOptions);
        recommendations.Should().NotBeNull();

        recommendations!.Should().ContainSingle(r =>
            r.NodeType == RecommendationNodeType.QuestionStyle &&
            r.NodeId == styleNewGen.Id &&
            r.Code == QuestionStyleDefaults.Codes.NewGeneration &&
            r.ClassificationState == AcademicClassificationState.Critical &&
            r.RecommendationType == AcademicRecommendationType.RemediateCritical &&
            r.SubjectId == null &&
            r.TopicId == null);

        recommendations.Should().NotContain(r => r.NodeType == RecommendationNodeType.QuestionStyle && r.Code == QuestionStyleDefaults.Codes.Conceptual);
        recommendations.Should().NotContain(r => r.NodeType == RecommendationNodeType.QuestionStyle && r.Code == QuestionStyleDefaults.Codes.GraphTable);

        // 2. Verify /study-plan proposal endpoint schedules QuestionStyle sessions
        var planResp = await client.GetAsync($"/api/v1/students/{student.Id}/academic-intelligence/study-plan?planDays=3&dailyStudyMinutes=60");
        planResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var proposal = await planResp.Content.ReadFromJsonAsync<AcademicStudyPlanDto>(JsonOptions);
        proposal.Should().NotBeNull();
        proposal!.Days.SelectMany(d => d.Sessions).Should().Contain(s =>
            s.NodeType == RecommendationNodeType.QuestionStyle &&
            s.NodeId == styleNewGen.Id &&
            s.Code == QuestionStyleDefaults.Codes.NewGeneration);

        // 3. Verify POST /study-plans/from-academic-intelligence persists QuestionStyle StudyTasks in MSSQL without FK errors
        var acceptResp = await client.PostAsJsonAsync(
            $"/api/v1/students/{student.Id}/study-plans/from-academic-intelligence",
            new AcceptAcademicStudyPlanCommand(student.Id, PlanDays: 3, DailyStudyMinutes: 60));
        acceptResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var acceptedPlan = await acceptResp.Content.ReadFromJsonAsync<StudyPlanDetailDto>(JsonOptions);
        acceptedPlan.Should().NotBeNull();
        acceptedPlan!.Tasks.Should().Contain(t =>
            t.NodeType == RecommendationNodeType.QuestionStyle &&
            t.NodeId == styleNewGen.Id &&
            t.TitleSnapshot == styleNewGen.Name &&
            t.SubjectId == null &&
            t.TopicId == null &&
            t.SubTopicId == null &&
            t.LearningOutcomeId == null);

        // Verify in real MSSQL DB
        using var verifyDb = Db();
        var dbStyleTasks = await verifyDb.Set<EgitimPlatform.Modules.Planning.Entities.StudyTask>()
            .Where(t => t.StudyPlanId == acceptedPlan.Plan.Id && t.NodeType == RecommendationNodeType.QuestionStyle)
            .ToListAsync();
        dbStyleTasks.Should().NotBeEmpty();
        dbStyleTasks.Should().OnlyContain(t => t.NodeId == styleNewGen.Id && t.SubjectId == null && t.TopicId == null);

        // 4. Verify /ai-summary endpoint includes QuestionStyle strengths & improvement areas
        var aiResp = await client.GetAsync($"/api/v1/students/{student.Id}/academic-intelligence/ai-summary?audience=student");
        aiResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var aiSummary = await aiResp.Content.ReadFromJsonAsync<AcademicAiSummaryDto>(JsonOptions);
        aiSummary.Should().NotBeNull();
        aiSummary!.IsAvailable.Should().BeTrue();
        aiSummary.Strengths.Should().Contain($"{styleConcept.Name} (Soru Tarzı)");
        aiSummary.ImprovementAreas.Should().Contain($"{styleNewGen.Name} (Soru Tarzı)");
        aiSummary.SuggestedFocus.Should().Contain(styleNewGen.Name);
    }
}
