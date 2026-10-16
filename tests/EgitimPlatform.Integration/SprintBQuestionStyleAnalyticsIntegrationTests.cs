using System.Net;
using System.Net.Http.Json;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Identity.Features.Login;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Questions.Constants;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class SprintBQuestionStyleAnalyticsIntegrationTests(IntegrationTestFactory factory)
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(factory.ConnectionString).Options);

    private static string Code() => "SQB-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

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
    public async Task QuestionStyleAnalytics_RealMssql_CalculatesCorrectMetrics_AndPreservesExistingAnalytics()
    {
        using var db = Db();

        // 1. Get Institution of admin-a
        var adminUser = await db.Set<EgitimPlatform.Modules.Identity.Entities.ApplicationUser>()
            .FirstAsync(u => u.Email == "admin-a@test.local");
        var instId = adminUser.InstitutionId!.Value;

        // 2. Academic hierarchy
        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Problemler" };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        // 3. Student
        var student = new Student
        {
            InstitutionId = instId,
            UserId = Guid.NewGuid(),
            FirstName = "Style",
            LastName = "Student"
        };
        db.Add(student);
        await db.SaveChangesAsync();

        // 4. Question Styles
        var styles = await db.Set<QuestionStyle>().OrderBy(s => s.DisplayOrder).ToListAsync();
        styles.Should().HaveCount(8);

        var styleNewGen = styles.First(s => s.Code == QuestionStyleDefaults.Codes.NewGeneration);
        var styleGraph = styles.First(s => s.Code == QuestionStyleDefaults.Codes.GraphTable);
        var styleCalc = styles.First(s => s.Code == QuestionStyleDefaults.Codes.Calculation);

        // 5. Questions
        // q1: NEW_GENERATION + GRAPH_TABLE (multi-style)
        var q1 = new Question { InstitutionId = instId, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1 Multi", Status = QuestionStatus.Published };
        q1.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = q1.Id, QuestionStyleId = styleNewGen.Id });
        q1.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = q1.Id, QuestionStyleId = styleGraph.Id });

        // q2: NEW_GENERATION + CALCULATION (multi-style)
        var q2 = new Question { InstitutionId = instId, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q2 Multi", Status = QuestionStatus.Published };
        q2.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = q2.Id, QuestionStyleId = styleNewGen.Id });
        q2.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = q2.Id, QuestionStyleId = styleCalc.Id });

        // q3: CALCULATION only (single-style)
        var q3 = new Question { InstitutionId = instId, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q3 Single", Status = QuestionStatus.Published };
        q3.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = q3.Id, QuestionStyleId = styleCalc.Id });

        // q4: zero styles
        var q4 = new Question { InstitutionId = instId, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q4 NoStyle", Status = QuestionStatus.Published };

        // q5: NEW_GENERATION only to reach >= 5 total questions for NEW_GENERATION
        var q5 = new Question { InstitutionId = instId, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q5 Single", Status = QuestionStatus.Published };
        q5.StyleAssignments.Add(new QuestionStyleAssignment { QuestionId = q5.Id, QuestionStyleId = styleNewGen.Id });

        db.AddRange(q1, q2, q3, q4, q5);
        await db.SaveChangesAsync();

        // 6. Exam and ExamQuestions
        var exam = new Exam { InstitutionId = instId, ExamTypeId = examType.Id, Code = Code(), Title = "Deneme Sınavı 1" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = instId, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 5 };
        db.Add(section);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = instId, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, Order = 1, CorrectOption = "A" };
        var eq2 = new ExamQuestion { InstitutionId = instId, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q2.Id, QuestionNumber = 2, Order = 2, CorrectOption = "B" };
        var eq3 = new ExamQuestion { InstitutionId = instId, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q3.Id, QuestionNumber = 3, Order = 3, CorrectOption = "C" };
        var eq4 = new ExamQuestion { InstitutionId = instId, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q4.Id, QuestionNumber = 4, Order = 4, CorrectOption = "D" };
        var eq5 = new ExamQuestion { InstitutionId = instId, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q5.Id, QuestionNumber = 5, Order = 5, CorrectOption = "E" };
        db.AddRange(eq1, eq2, eq3, eq4, eq5);
        await db.SaveChangesAsync();

        // 7. Attempt 1 (Finalized)
        var attempt1 = new StudentExamAttempt
        {
            InstitutionId = instId,
            ExamId = exam.Id,
            StudentId = student.Id,
            Status = AttemptStatus.Finalized,
            AttemptNumber = 1,
            IsRawAvailable = true,
            TakenAt = DateTimeOffset.UtcNow.AddDays(-3)
        };
        db.Add(attempt1);
        await db.SaveChangesAsync();

        // Attempt 1 question results:
        // q1 (NewGen, Graph): Correct
        // q2 (NewGen, Calc): Correct
        // q3 (Calc): Incorrect
        // q4 (NoStyle): Correct
        // q5 (NewGen): Correct
        db.AddRange(
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt1.Id, ExamQuestionId = eq1.Id, QuestionId = q1.Id, SelectedOption = "A", IsCorrect = true, IsBlank = false, NetContribution = 1m },
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt1.Id, ExamQuestionId = eq2.Id, QuestionId = q2.Id, SelectedOption = "B", IsCorrect = true, IsBlank = false, NetContribution = 1m },
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt1.Id, ExamQuestionId = eq3.Id, QuestionId = q3.Id, SelectedOption = "A", IsCorrect = false, IsBlank = false, NetContribution = -0.25m },
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt1.Id, ExamQuestionId = eq4.Id, QuestionId = q4.Id, SelectedOption = "D", IsCorrect = true, IsBlank = false, NetContribution = 1m },
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt1.Id, ExamQuestionId = eq5.Id, QuestionId = q5.Id, SelectedOption = "E", IsCorrect = true, IsBlank = false, NetContribution = 1m }
        );

        // Add Subject and Topic results for attempt 1
        db.Add(new StudentExamSubjectResult { InstitutionId = instId, StudentExamAttemptId = attempt1.Id, ExamSectionId = section.Id, Correct = 4, Wrong = 1, Blank = 0, Net = 3.75m });
        db.Add(new StudentExamTopicResult { InstitutionId = instId, StudentExamAttemptId = attempt1.Id, ExamSectionId = section.Id, TopicId = topic.Id, Correct = 4, Wrong = 1, Blank = 0, Net = 3.75m });
        await db.SaveChangesAsync();

        // 8. Attempt 2 (Finalized)
        var attempt2 = new StudentExamAttempt
        {
            InstitutionId = instId,
            ExamId = exam.Id,
            StudentId = student.Id,
            Status = AttemptStatus.Finalized,
            AttemptNumber = 2,
            IsRawAvailable = true,
            TakenAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        db.Add(attempt2);
        await db.SaveChangesAsync();

        // Attempt 2 question results:
        // q1 (NewGen, Graph): Correct
        // q2 (NewGen, Calc): Incorrect
        // q3 (Calc): Correct
        // q4 (NoStyle): Blank
        // q5 (NewGen): Correct
        db.AddRange(
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt2.Id, ExamQuestionId = eq1.Id, QuestionId = q1.Id, SelectedOption = "A", IsCorrect = true, IsBlank = false, NetContribution = 1m },
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt2.Id, ExamQuestionId = eq2.Id, QuestionId = q2.Id, SelectedOption = "A", IsCorrect = false, IsBlank = false, NetContribution = -0.25m },
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt2.Id, ExamQuestionId = eq3.Id, QuestionId = q3.Id, SelectedOption = "C", IsCorrect = true, IsBlank = false, NetContribution = 1m },
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt2.Id, ExamQuestionId = eq4.Id, QuestionId = q4.Id, SelectedOption = null, IsCorrect = null, IsBlank = true, NetContribution = 0m },
            new StudentExamQuestionResult { InstitutionId = instId, StudentExamAttemptId = attempt2.Id, ExamQuestionId = eq5.Id, QuestionId = q5.Id, SelectedOption = "E", IsCorrect = true, IsBlank = false, NetContribution = 1m }
        );

        // Add Subject and Topic results for attempt 2
        db.Add(new StudentExamSubjectResult { InstitutionId = instId, StudentExamAttemptId = attempt2.Id, ExamSectionId = section.Id, Correct = 3, Wrong = 1, Blank = 1, Net = 2.75m });
        db.Add(new StudentExamTopicResult { InstitutionId = instId, StudentExamAttemptId = attempt2.Id, ExamSectionId = section.Id, TopicId = topic.Id, Correct = 3, Wrong = 1, Blank = 1, Net = 2.75m });
        await db.SaveChangesAsync();

        // 9. Call Question Style Analytics API Endpoint
        var client = await GetAuthenticatedAdminClientAsync();

        var analyticsResponse = await client.GetAsync($"/api/v1/students/{student.Id}/academic-intelligence/question-styles");
        analyticsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var analytics = await analyticsResponse.Content.ReadFromJsonAsync<List<StudentQuestionStyleAnalyticsDto>>();
        analytics.Should().NotBeNull();
        analytics!.Should().HaveCount(8); // all 8 controlled styles

        // Verify NEW_GENERATION metrics:
        // q1, q2, q5 appeared in attempt 1 (3 questions, all correct)
        // q1, q2, q5 appeared in attempt 2 (3 questions: 2 correct, 1 wrong)
        // Total = 6 questions, 5 correct, 1 wrong, 0 blank
        // SuccessRate = 5/6 = 0.8333 -> Strong!
        // ObservationCount = 2 attempts
        var newGenDto = analytics!.First(a => a.Code == QuestionStyleDefaults.Codes.NewGeneration);
        newGenDto.TotalQuestions.Should().Be(6);
        newGenDto.CorrectCount.Should().Be(5);
        newGenDto.IncorrectCount.Should().Be(1);
        newGenDto.BlankCount.Should().Be(0);
        newGenDto.ObservationCount.Should().Be(2);
        newGenDto.SuccessRate.Should().Be(0.8333m);
        newGenDto.Accuracy.Should().Be(0.8333m);
        newGenDto.Coverage.Should().Be(1.0000m);
        newGenDto.ClassificationState.Should().Be(AcademicClassificationState.Strong);

        // Verify GRAPH_TABLE metrics:
        // q1 appeared in attempt 1 (1 correct) and attempt 2 (1 correct)
        // Total = 2 questions, 2 correct. TotalQuestions = 2 < 5 -> InsufficientData
        var graphDto = analytics!.First(a => a.Code == QuestionStyleDefaults.Codes.GraphTable);
        graphDto.TotalQuestions.Should().Be(2);
        graphDto.CorrectCount.Should().Be(2);
        graphDto.ObservationCount.Should().Be(2);
        graphDto.ClassificationState.Should().Be(AcademicClassificationState.InsufficientData);

        // Verify CALCULATION metrics:
        // q2 and q3 appeared in attempt 1 (q2 correct, q3 wrong -> 1 correct, 1 wrong)
        // q2 and q3 appeared in attempt 2 (q2 wrong, q3 correct -> 1 correct, 1 wrong)
        // Total = 4 questions, 2 correct, 2 wrong. TotalQuestions = 4 < 5 -> InsufficientData
        var calcDto = analytics!.First(a => a.Code == QuestionStyleDefaults.Codes.Calculation);
        calcDto.TotalQuestions.Should().Be(4);
        calcDto.CorrectCount.Should().Be(2);
        calcDto.IncorrectCount.Should().Be(2);
        calcDto.ObservationCount.Should().Be(2);
        calcDto.ClassificationState.Should().Be(AcademicClassificationState.InsufficientData);

        // 10. Call Question Style Trends API Endpoint
        var trendsResponse = await client.GetAsync($"/api/v1/students/{student.Id}/academic-intelligence/question-style-trends?windowSize=2");
        trendsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var trends = await trendsResponse.Content.ReadFromJsonAsync<List<QuestionStyleTrendDto>>();
        trends.Should().NotBeNull();
        trends!.Should().HaveCount(8);

        var newGenTrend = trends!.First(t => t.Code == QuestionStyleDefaults.Codes.NewGeneration);
        newGenTrend.TotalObservations.Should().Be(2);
        newGenTrend.TimeSeries.Should().HaveCount(2);

        // 11. Verify Existing Subject and Topic Analytics remain 100% intact and working
        var subjectResp = await client.GetAsync($"/api/v1/students/{student.Id}/academic-intelligence/subjects");
        subjectResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var subjectList = await subjectResp.Content.ReadFromJsonAsync<List<StudentSubjectPerformanceDto>>();
        subjectList.Should().NotBeNull();
        subjectList!.Should().Contain(s => s.SubjectId == subject.Id);

        var topicResp = await client.GetAsync($"/api/v1/students/{student.Id}/academic-intelligence/topics");
        topicResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var topicList = await topicResp.Content.ReadFromJsonAsync<List<StudentTopicPerformanceDto>>();
        topicList.Should().NotBeNull();
        topicList!.Should().Contain(t => t.TopicId == topic.Id);
    }
}
