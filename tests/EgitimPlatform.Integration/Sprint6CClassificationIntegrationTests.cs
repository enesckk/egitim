using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Sprint6CClassificationIntegrationTests(IntegrationTestFactory factory)
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

    private static string Code() => "S6C-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task GetSubjectClassifications_AggregatesAndRawAttempts_ReturnsValidClassification()
    {
        _ = ConnectionString;
        using var db = Db();
        var inst = new Institution { Name = "Inst S6C Classifications" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-S6C-1", FirstName = "Mehmet", LastName = "A" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var resourceAccess = Substitute.For<IExamResourceAccess>();

        // 6 attempts
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
                IsRawAvailable = true
            };
            db.Add(attempt);
            await db.SaveChangesAsync();

            int correct = i <= 3 ? 3 : 9; // Previous: 30%, Recent: 90%
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

        var trendHandler = new GetStudentAcademicTrendHandler(db, new DummyAcademicCatalog(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);

        var query = new AcademicClassificationQuery(student.Id, WindowSize: 3);
        var classifications = await classificationHandler.GetSubjectClassificationsAsync(query);

        Assert.Single(classifications);
        var subjectClassification = classifications[0];

        Assert.Equal(subject.Id, subjectClassification.SubjectId);
        Assert.True(subjectClassification.HasEnoughData);
        Assert.Equal(ClassificationBasis.RecentWindow, subjectClassification.Basis);
        Assert.Equal(0.9000m, subjectClassification.SuccessRate);
        Assert.Equal(AcademicClassificationState.Strong, subjectClassification.State);
        Assert.Equal(TrendDirection.Increasing, subjectClassification.TrendDirection);
        Assert.Equal(0.6000m, subjectClassification.SuccessRateDelta);
        Assert.Equal("v1", subjectClassification.PolicyVersion);
        Assert.Equal(ClassificationInsufficiencyReason.None, subjectClassification.InsufficiencyReason);
    }

    [Fact]
    public async Task GetTopicClassifications_RawAttemptsOnly_ExcludesAggregateAttempts()
    {
        _ = ConnectionString;
        using var db = Db();
        var inst = new Institution { Name = "Inst S6C Topic" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-S6C-2", FirstName = "Zeynep", LastName = "B" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "AYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = Code(), Name = "Türev", SubjectId = subject.Id };
        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var resourceAccess = Substitute.For<IExamResourceAccess>();

        // 2 raw attempts (TopicMinObservations = 2, TopicMinQuestions = 5)
        for (int i = 1; i <= 2; i++)
        {
            var exam = new Exam { InstitutionId = inst.Id, Title = $"AYT Deneme {i}", ExamTypeId = examType.Id, ExamDate = now.AddDays(-5 + i) };
            db.Add(exam);
            await db.SaveChangesAsync();

            var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, DisplayOrder = 1, QuestionCount = 10 };
            db.Add(section);
            await db.SaveChangesAsync();

            var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, TakenAt = exam.ExamDate, Status = AttemptStatus.Finalized, IsRawAvailable = true };
            db.Add(attempt);
            await db.SaveChangesAsync();

            var tr = new StudentExamTopicResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamSectionId = section.Id, TopicId = topic.Id, Correct = 3, Wrong = 2, Blank = 0, Net = 2.5m };
            db.Add(tr);
            await db.SaveChangesAsync();
        }

        var trendHandler = new GetStudentAcademicTrendHandler(db, new DummyAcademicCatalog(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);

        var query = new AcademicClassificationQuery(student.Id, WindowSize: 3);
        var classifications = await classificationHandler.GetTopicClassificationsAsync(query);

        Assert.Single(classifications);
        var topicClassification = classifications[0];

        Assert.Equal(topic.Id, topicClassification.TopicId);
        Assert.True(topicClassification.HasEnoughData);
        Assert.Equal(0.6000m, topicClassification.SuccessRate); // 6 / 10 = 0.6000 -> Developing
        Assert.Equal(AcademicClassificationState.Developing, topicClassification.State);
        Assert.Equal(ClassificationInsufficiencyReason.None, topicClassification.InsufficiencyReason);
    }
}
