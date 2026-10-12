using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Sprint6BTrendIntegrationTests(IntegrationTestFactory factory)
{
    private string ConnectionString => factory.ConnectionString;

    private class DummyAcademicCatalog : IAcademicCatalog
    {
        public Task<AcademicReferenceDto?> GetExamTypeAsync(Guid id, CancellationToken ct = default) => Task.FromResult<AcademicReferenceDto?>(null);
        public Task<IReadOnlyList<AcademicReferenceDto>> GetExamTypesAsync(IEnumerable<Guid> ids, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AcademicReferenceDto>>([]);
        public Task<bool> SubjectExistsAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateExamTypeSubjectAsync(Guid examTypeId, Guid subjectId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateSubjectTopicsAsync(Guid subjectId, IEnumerable<Guid> topicIds, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyDictionary<Guid, string>> GetSubjectNamesAsync(IEnumerable<Guid> subjectIds, CancellationToken ct = default)
        {
            var dict = subjectIds.ToDictionary(id => id, id => "Matematik");
            return Task.FromResult<IReadOnlyDictionary<Guid, string>>(dict);
        }
        public Task<IReadOnlyDictionary<Guid, string>> GetTopicNamesAsync(IEnumerable<Guid> topicIds, CancellationToken ct = default)
        {
            var dict = topicIds.ToDictionary(id => id, id => "Türev");
            return Task.FromResult<IReadOnlyDictionary<Guid, string>>(dict);
        }
        public Task<bool> ValidateTopicSubTopicAsync(Guid topicId, Guid subTopicId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateLearningOutcomeHierarchyAsync(Guid subjectId, Guid topicId, Guid? subTopicId, Guid learningOutcomeId, CancellationToken ct = default) => Task.FromResult(true);
    }

    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static string Code() => "S6B-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task GetSubjectTrends_AggregatesAndRawAttempts_OrdersByTakenAt()
    {
        _ = ConnectionString;
        using var db = Db();
        var inst = new Institution { Name = "Inst S6B Trends" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-S6B-TR1", FirstName = "Ali", LastName = "Y" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var resourceAccess = Substitute.For<IExamResourceAccess>();

        // Create 6 attempts on different historical dates
        for (int i = 1; i <= 6; i++)
        {
            var exam = new Exam
            {
                InstitutionId = inst.Id,
                Title = $"TYT Deneme {i}",
                ExamTypeId = examType.Id,
                ExamDate = now.AddDays(-10 + i)
            };
            db.Add(exam);
            await db.SaveChangesAsync();

            var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, DisplayOrder = 1, QuestionCount = 10 };
            db.Add(section);
            await db.SaveChangesAsync();

            var attempt = new StudentExamAttempt
            {
                InstitutionId = inst.Id,
                ExamId = exam.Id,
                StudentId = student.Id,
                AttemptNumber = 1,
                TakenAt = exam.ExamDate,
                Status = AttemptStatus.Finalized,
                IsRawAvailable = i % 2 == 0
            };
            db.Add(attempt);
            await db.SaveChangesAsync();

            int correct = i <= 3 ? 4 : 8;
            var sr = new StudentExamSubjectResult
            {
                InstitutionId = inst.Id,
                StudentExamAttemptId = attempt.Id,
                ExamSectionId = section.Id,
                Correct = correct,
                Wrong = 10 - correct,
                Blank = 0,
                Net = correct - ((10 - correct) * 0.25m)
            };
            db.Add(sr);
            await db.SaveChangesAsync();
        }

        var handler = new GetStudentAcademicTrendHandler(db, new DummyAcademicCatalog(), resourceAccess);
        var query = new AcademicTrendQuery(student.Id, WindowSize: 3);

        var trends = await handler.GetSubjectTrendsAsync(query);

        Assert.Single(trends);
        var subjectTrend = trends[0];

        Assert.Equal(subject.Id, subjectTrend.SubjectId);
        Assert.Equal(6, subjectTrend.TotalObservations);
        Assert.True(subjectTrend.HasEnoughData);
        Assert.NotNull(subjectTrend.PreviousWindow);
        Assert.NotNull(subjectTrend.RecentWindow);

        Assert.Equal(0.4000m, subjectTrend.PreviousWindow.SuccessRate);
        Assert.Equal(0.8000m, subjectTrend.RecentWindow.SuccessRate);
        Assert.Equal(0.4000m, subjectTrend.SuccessRateDelta);
        Assert.Equal(TrendDirection.Increasing, subjectTrend.Direction);

        for (int i = 0; i < 5; i++)
        {
            Assert.True(subjectTrend.TimeSeries[i].TakenAt <= subjectTrend.TimeSeries[i + 1].TakenAt);
        }
    }

    [Fact]
    public async Task GetSubjectTrends_MultiSectionInSameAttempt_AggregatesIntoOneObservationPoint()
    {
        _ = ConnectionString;
        using var db = Db();
        var inst = new Institution { Name = "Inst S6B MultiSection" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-S6B-TR3", FirstName = "Can", LastName = "D" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "LGS" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var resourceAccess = Substitute.For<IExamResourceAccess>();

        var exam = new Exam { InstitutionId = inst.Id, Title = "LGS Deneme 1", ExamTypeId = examType.Id, ExamDate = now };
        db.Add(exam);
        await db.SaveChangesAsync();

        // 2 sections for the same Subject in 1 Exam
        var section1 = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, DisplayOrder = 1, QuestionCount = 10 };
        var section2 = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, DisplayOrder = 2, QuestionCount = 10 };
        db.AddRange(section1, section2);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, TakenAt = exam.ExamDate, Status = AttemptStatus.Finalized, IsRawAvailable = false };
        db.Add(attempt);
        await db.SaveChangesAsync();

        // Section 1: 5 correct, Section 2: 7 correct
        var sr1 = new StudentExamSubjectResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamSectionId = section1.Id, Correct = 5, Wrong = 5, Blank = 0, Net = 3.75m };
        var sr2 = new StudentExamSubjectResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamSectionId = section2.Id, Correct = 7, Wrong = 3, Blank = 0, Net = 6.25m };
        db.AddRange(sr1, sr2);
        await db.SaveChangesAsync();

        var handler = new GetStudentAcademicTrendHandler(db, new DummyAcademicCatalog(), resourceAccess);
        var query = new AcademicTrendQuery(student.Id, WindowSize: 3);

        var trends = await handler.GetSubjectTrendsAsync(query);

        Assert.Single(trends);
        var subjectTrend = trends[0];

        // Should have ONE aggregated observation point for this attempt (Total 12 correct out of 20 questions)
        Assert.Equal(1, subjectTrend.TotalObservations);
        Assert.Equal(20, subjectTrend.TimeSeries[0].TotalQuestions);
        Assert.Equal(12, subjectTrend.TimeSeries[0].Correct);
        Assert.Equal(10.00m, subjectTrend.TimeSeries[0].Net);
    }

    [Fact]
    public async Task GetTopicTrends_RawAttemptsOnly_ExcludesAggregateAttemptsAndMissingTopics()
    {
        _ = ConnectionString;
        using var db = Db();
        var inst = new Institution { Name = "Inst S6B Topic Trends" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-S6B-TR2", FirstName = "Ayşe", LastName = "K" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "AYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = Code(), Name = "Türev", SubjectId = subject.Id };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var resourceAccess = Substitute.For<IExamResourceAccess>();

        // Attempt 1: Aggregate-only (IsRawAvailable = false) -> MUST BE EXCLUDED FROM TOPIC TREND
        var examAgg = new Exam { InstitutionId = inst.Id, Title = "Aggregate Exam", ExamTypeId = examType.Id, ExamDate = now.AddDays(-5) };
        db.Add(examAgg);
        await db.SaveChangesAsync();

        var sectionAgg = new ExamSection { InstitutionId = inst.Id, ExamId = examAgg.Id, SubjectId = subject.Id, DisplayOrder = 1, QuestionCount = 10 };
        db.Add(sectionAgg);
        await db.SaveChangesAsync();

        var attAgg = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = examAgg.Id, StudentId = student.Id, TakenAt = examAgg.ExamDate, Status = AttemptStatus.Finalized, IsRawAvailable = false };
        db.Add(attAgg);
        await db.SaveChangesAsync();

        // Attempt 2: Raw attempt with Topic result
        var examRaw = new Exam { InstitutionId = inst.Id, Title = "Raw Exam 1", ExamTypeId = examType.Id, ExamDate = now.AddDays(-3) };
        db.Add(examRaw);
        await db.SaveChangesAsync();

        var sectionRaw = new ExamSection { InstitutionId = inst.Id, ExamId = examRaw.Id, SubjectId = subject.Id, DisplayOrder = 1, QuestionCount = 10 };
        db.Add(sectionRaw);
        await db.SaveChangesAsync();

        var attRaw = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = examRaw.Id, StudentId = student.Id, TakenAt = examRaw.ExamDate, Status = AttemptStatus.Finalized, IsRawAvailable = true };
        db.Add(attRaw);
        await db.SaveChangesAsync();

        var tr = new StudentExamTopicResult { InstitutionId = inst.Id, StudentExamAttemptId = attRaw.Id, ExamSectionId = sectionRaw.Id, TopicId = topic.Id, Correct = 5, Wrong = 0, Blank = 0, Net = 5m };
        db.Add(tr);
        await db.SaveChangesAsync();

        var handler = new GetStudentAcademicTrendHandler(db, new DummyAcademicCatalog(), resourceAccess);
        var query = new AcademicTrendQuery(student.Id, WindowSize: 3);

        var trends = await handler.GetTopicTrendsAsync(query);

        Assert.Single(trends);
        var topicTrend = trends[0];

        Assert.Equal(topic.Id, topicTrend.TopicId);
        Assert.Equal(1, topicTrend.TotalObservations);
        Assert.False(topicTrend.HasEnoughData);
        Assert.Null(topicTrend.Direction);
    }
}
