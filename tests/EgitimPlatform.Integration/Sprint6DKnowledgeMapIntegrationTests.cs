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
public class Sprint6DKnowledgeMapIntegrationTests(IntegrationTestFactory factory)
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

    private static string Code() => "S6D-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task GetKnowledgeMap_Integration_AssemblesFullTreeWithEvidenceAndClassification()
    {
        _ = ConnectionString;
        using var db = Db();
        var inst = new Institution { Name = "Inst S6D KM" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-S6D-1", FirstName = "Hakan", LastName = "M" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = Code(), Name = "Türev", SubjectId = subject.Id };
        var subTopic = new SubTopic { Code = Code(), Name = "Türev Kuralları", TopicId = topic.Id };
        var lo = new LearningOutcome { Code = Code(), Name = "Türev Alma", SubTopicId = subTopic.Id };

        db.AddRange(examType, subject, topic, subTopic, lo);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var resourceAccess = Substitute.For<IExamResourceAccess>();

        // Add 2 attempt subject results (satisfies SubjectMinObservations = 2, SubjectMinQuestions = 10)
        for (int i = 1; i <= 2; i++)
        {
            var exam = new Exam { InstitutionId = inst.Id, Title = $"TYT Deneme {i}", ExamTypeId = examType.Id, ExamDate = now.AddDays(-5 + i) };
            db.Add(exam);
            await db.SaveChangesAsync();

            var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, DisplayOrder = 1, QuestionCount = 10 };
            db.Add(section);
            await db.SaveChangesAsync();

            var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, TakenAt = exam.ExamDate, Status = AttemptStatus.Finalized, IsRawAvailable = true };
            db.Add(attempt);
            await db.SaveChangesAsync();

            var sr = new StudentExamSubjectResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamSectionId = section.Id, Correct = 9, Wrong = 1, Blank = 0, Net = 8.75m };
            db.Add(sr);
            await db.SaveChangesAsync();
        }

        var trendHandler = new GetStudentAcademicTrendHandler(db, new DummyAcademicCatalog(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);

        var query = new AcademicClassificationQuery(student.Id, ExamTypeId: examType.Id);
        var km = await mapHandler.GetKnowledgeMapAsync(query);

        Assert.Equal(student.Id, km.StudentId);
        Assert.Single(km.Subjects);
        var subjectNode = km.Subjects[0];

        Assert.Equal(subject.Id, subjectNode.Id);
        Assert.True(subjectNode.HasEvidence);
        Assert.True(subjectNode.HasEnoughData);
        Assert.Equal(AcademicClassificationState.Strong, subjectNode.State); // 18/20 = 0.90 -> Strong
        Assert.Equal(0.9000m, subjectNode.SuccessRate);

        Assert.Single(subjectNode.Topics);
        var topicNode = subjectNode.Topics[0];
        Assert.Equal(topic.Id, topicNode.Id);

        Assert.Single(topicNode.SubTopics);
        var subTopicNode = topicNode.SubTopics[0];
        Assert.Equal(subTopic.Id, subTopicNode.Id);

        Assert.Single(subTopicNode.LearningOutcomes);
        var loNode = subTopicNode.LearningOutcomes[0];
        Assert.Equal(lo.Id, loNode.Id);
        Assert.False(loNode.HasEvidence);
        Assert.Null(loNode.SuccessRate);
    }

    [Fact]
    public async Task GetKnowledgeMap_SoftDeletedTopic_PreservesHistoricalTopicNode()
    {
        _ = ConnectionString;
        using var db = Db();
        var inst = new Institution { Name = "Inst SoftDelete Test" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-SD-1", FirstName = "Burak", LastName = "C" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = Code(), Name = "Eski Konu", SubjectId = subject.Id, IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow };

        db.AddRange(examType, subject, topic);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var resourceAccess = Substitute.For<IExamResourceAccess>();

        // Raw attempt referencing soft-deleted topic
        var exam = new Exam { InstitutionId = inst.Id, Title = "Deneme", ExamTypeId = examType.Id, ExamDate = now };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, DisplayOrder = 1, QuestionCount = 10 };
        db.Add(section);
        await db.SaveChangesAsync();

        var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, TakenAt = exam.ExamDate, Status = AttemptStatus.Finalized, IsRawAvailable = true };
        db.Add(attempt);
        await db.SaveChangesAsync();

        var tr = new StudentExamTopicResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamSectionId = section.Id, TopicId = topic.Id, Correct = 5, Wrong = 0, Blank = 0, Net = 5m };
        db.Add(tr);
        await db.SaveChangesAsync();

        var trendHandler = new GetStudentAcademicTrendHandler(db, new DummyAcademicCatalog(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);

        var query = new AcademicClassificationQuery(student.Id, ExamTypeId: examType.Id);
        var km = await mapHandler.GetKnowledgeMapAsync(query);

        Assert.Single(km.Subjects);
        var topicNode = km.Subjects[0].Topics.FirstOrDefault(t => t.Id == topic.Id);
        Assert.NotNull(topicNode);
        Assert.Equal("Eski Konu", topicNode.Name);
    }
}
