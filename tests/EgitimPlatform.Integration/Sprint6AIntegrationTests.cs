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
public class Sprint6AIntegrationTests(IntegrationTestFactory factory)
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
            var dict = subjectIds.ToDictionary(id => id, id => "Fizik");
            return Task.FromResult<IReadOnlyDictionary<Guid, string>>(dict);
        }
        public Task<IReadOnlyDictionary<Guid, string>> GetTopicNamesAsync(IEnumerable<Guid> topicIds, CancellationToken ct = default)
        {
            var dict = topicIds.ToDictionary(id => id, id => "Optik");
            return Task.FromResult<IReadOnlyDictionary<Guid, string>>(dict);
        }
        public Task<bool> ValidateTopicSubTopicAsync(Guid topicId, Guid subTopicId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateLearningOutcomeHierarchyAsync(Guid subjectId, Guid topicId, Guid? subTopicId, Guid learningOutcomeId, CancellationToken ct = default) => Task.FromResult(true);
    }

    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static string Code() => "S6A-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task AcademicIntelligence_FullFlow_CalculatesMetricsAndCoverage()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst S6A Intelligence" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-S6A-INT1", FirstName = "Hasan", LastName = "B" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "AYT" };
        var subject = new Subject { Code = Code(), Name = "Fizik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = Code(), Name = "Optik", SubjectId = subject.Id };
        var subTopic = new SubTopic { Code = Code(), Name = "Kırılma", TopicId = topic.Id };
        var learningOutcome = new LearningOutcome { Code = Code(), Name = "Işıkta Kırılma Yasaları", SubTopicId = subTopic.Id };

        db.AddRange(examType, subject, topic, subTopic, learningOutcome);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "S6A Integration Exam", WrongAnswerPenalty = 4.0m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 2, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        // 2 Questions tagged with LearningOutcome
        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, LearningOutcomeId = learningOutcome.Id, Content = "Optik Soru 1" };
        var q2 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, LearningOutcomeId = learningOutcome.Id, Content = "Optik Soru 2" };
        db.AddRange(q1, q2);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, CorrectOption = "A" };
        var eq2 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q2.Id, QuestionNumber = 2, CorrectOption = "B" };
        db.AddRange(eq1, eq2);
        await db.SaveChangesAsync();

        // Finalized Raw Attempt
        var attempt = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = inst.Id,
            ExamId = exam.Id,
            StudentId = student.Id,
            AttemptNumber = 1,
            TakenAt = DateTimeOffset.UtcNow,
            Status = AttemptStatus.Finalized,
            FinalizedAt = DateTimeOffset.UtcNow,
            IsRawAvailable = true,
            TotalCorrect = 2,
            TotalWrong = 0,
            TotalBlank = 0,
            TotalNet = 2.0m
        };
        var sr = new StudentExamSubjectResult { Id = Guid.NewGuid(), InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamSectionId = section.Id, Correct = 2, Wrong = 0, Blank = 0, Net = 2.0m };
        var tr = new StudentExamTopicResult { Id = Guid.NewGuid(), InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamSectionId = section.Id, TopicId = topic.Id, Correct = 2, Wrong = 0, Blank = 0, Net = 2.0m };

        var qr1 = new StudentExamQuestionResult { Id = Guid.NewGuid(), InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamQuestionId = eq1.Id, QuestionId = q1.Id, SelectedOption = "A", IsCorrect = true, IsBlank = false, NetContribution = 1.0m };
        var qr2 = new StudentExamQuestionResult { Id = Guid.NewGuid(), InstitutionId = inst.Id, StudentExamAttemptId = attempt.Id, ExamQuestionId = eq2.Id, QuestionId = q2.Id, SelectedOption = "B", IsCorrect = true, IsBlank = false, NetContribution = 1.0m };

        db.AddRange(attempt, sr, tr, qr1, qr2);
        await db.SaveChangesAsync();

        var handler = new GetStudentAcademicIntelligenceHandler(db, new DummyAcademicCatalog(), Substitute.For<IExamResourceAccess>());
        var query = new AcademicIntelligenceQuery(student.Id);

        // Act 1: Subject Performance
        var subjects = await handler.GetSubjectPerformanceAsync(query);
        Assert.Single(subjects);
        Assert.Equal(1.0000m, subjects[0].Accuracy);

        // Act 2: Topic Performance
        var topics = await handler.GetTopicPerformanceAsync(query);
        Assert.Single(topics);
        Assert.Equal(topic.Id, topics[0].TopicId);
        Assert.Equal(2, topics[0].TotalCorrect);

        // Act 3: Learning Outcome Performance
        var outcomes = await handler.GetLearningOutcomePerformanceAsync(query);
        Assert.Single(outcomes);
        Assert.Equal(learningOutcome.Id, outcomes[0].LearningOutcomeId);
        Assert.Equal(2, outcomes[0].TotalCorrect);

        // Act 4: Coverage Summary
        var coverage = await handler.GetAcademicCoverageSummaryAsync(student.Id);
        Assert.Equal(1, coverage.TotalFinalizedAttempts);
        Assert.Equal(1, coverage.RawAttemptsCount);
        Assert.Equal(100.00m, coverage.RawCoveragePercentage);
        Assert.Equal(2, coverage.TotalQuestionsEvaluated);
        Assert.Equal(2, coverage.TaggedQuestionsEvaluated);
        Assert.Equal(100.00m, coverage.LearningOutcomeTagCoveragePercentage);
    }
}
