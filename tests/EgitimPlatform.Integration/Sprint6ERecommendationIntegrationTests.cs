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
public class Sprint6ERecommendationIntegrationTests(IntegrationTestFactory factory)
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

    private static string Code() => "S6E-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task GetRecommendations_Integration_MultidisciplinaryPriorityOrdering()
    {
        _ = ConnectionString;
        using var db = Db();
        var inst = new Institution { Name = "Inst Rec Integration" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-S6E-1", FirstName = "Ahmet", LastName = "Y" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subjectMath = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var subjectTurk = new Subject { Code = Code(), Name = "Türkçe", ExamTypeId = examType.Id };

        db.AddRange(examType, subjectMath, subjectTurk);
        await db.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        var resourceAccess = Substitute.For<IExamResourceAccess>();

        // Math: Critical (2 correct / 10 per exam)
        // Turk: Weak (5 correct / 10 per exam)
        for (int i = 1; i <= 2; i++)
        {
            var exam = new Exam { InstitutionId = inst.Id, Title = $"TYT Deneme {i}", ExamTypeId = examType.Id, ExamDate = now.AddDays(-5 + i) };
            db.Add(exam);
            await db.SaveChangesAsync();

            var secMath = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subjectMath.Id, DisplayOrder = 1, QuestionCount = 10 };
            var secTurk = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subjectTurk.Id, DisplayOrder = 2, QuestionCount = 10 };
            db.AddRange(secMath, secTurk);
            await db.SaveChangesAsync();

            var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = student.Id, TakenAt = exam.ExamDate, Status = AttemptStatus.Finalized, IsRawAvailable = true };
            db.Add(attempt);
            await db.SaveChangesAsync();

            var srMath = new StudentExamSubjectResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamSectionId = secMath.Id, Correct = 2, Wrong = 8, Blank = 0, Net = 0m };
            var srTurk = new StudentExamSubjectResult { InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamSectionId = secTurk.Id, Correct = 5, Wrong = 5, Blank = 0, Net = 3.75m };
            db.AddRange(srMath, srTurk);
            await db.SaveChangesAsync();
        }

        var trendHandler = new GetStudentAcademicTrendHandler(db, new DummyAcademicCatalog(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);
        var recHandler = new GetStudentAcademicRecommendationHandler(resourceAccess, mapHandler);

        var query = new AcademicRecommendationQuery(student.Id, ExamTypeId: examType.Id);
        var recs = await recHandler.GetRecommendationsAsync(query);

        Assert.NotEmpty(recs);
        // Math (Critical) must be prioritized over Türkçe (Weak)
        var topRec = recs[0];
        Assert.Equal(subjectMath.Id, topRec.SubjectId);
        Assert.Equal(AcademicRecommendationType.RemediateCritical, topRec.RecommendationType);
        Assert.True(topRec.PriorityScore > recs.First(r => r.SubjectId == subjectTurk.Id).PriorityScore);
    }
}
