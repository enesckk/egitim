using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint6ERecommendationUnitTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void CalculatePriorityScore_CriticalOverWeak_CriticalGetsHigherScore()
    {
        int scoreCritical = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.Critical,
            TrendDirection.Stable,
            0.30m,
            RecommendationNodeType.LearningOutcome
        );

        int scoreWeak = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.Weak,
            TrendDirection.Stable,
            0.50m,
            RecommendationNodeType.LearningOutcome
        );

        Assert.True(scoreCritical > scoreWeak);
    }

    [Fact]
    public void CalculatePriorityScore_TrendModifier_DecreasingGetsBoost()
    {
        int scoreDecreasing = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.Weak,
            TrendDirection.Decreasing,
            0.50m,
            RecommendationNodeType.Topic
        );

        int scoreIncreasing = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.Weak,
            TrendDirection.Increasing,
            0.50m,
            RecommendationNodeType.Topic
        );

        Assert.True(scoreDecreasing > scoreIncreasing);
        Assert.Equal(15, scoreDecreasing - scoreIncreasing);
    }

    [Fact]
    public void CalculatePriorityScore_ScoreClamping_ClampsBetween0And100()
    {
        int scoreClampedUpper = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.Critical,
            TrendDirection.Decreasing,
            0.00m,
            RecommendationNodeType.LearningOutcome
        );

        int scoreClampedLower = AcademicRecommendationCalculator.CalculatePriorityScore(
            AcademicClassificationState.InsufficientData,
            TrendDirection.Increasing,
            1.00m,
            RecommendationNodeType.Subject
        );

        Assert.InRange(scoreClampedUpper, 0, 100);
        Assert.Equal(0, scoreClampedLower);
    }

    [Fact]
    public async Task GetRecommendations_SpecificitySuppression_LOPreferredOverParentTopic()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Rec Test" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = "TYT", Name = "TYT" };
        var subject = new Subject { Code = "MAT", Name = "Matematik", ExamTypeId = examType.Id };
        var topic = new Topic { Code = "MAT-TOP-1", Name = "Türev", SubjectId = subject.Id };
        var subTopic = new SubTopic { Code = "MAT-SUB-1", Name = "Türev Kuralları", TopicId = topic.Id };
        var lo = new LearningOutcome { Code = "MAT.11.1", Name = "Türev Alma", SubTopicId = subTopic.Id };

        db.AddRange(examType, subject, topic, subTopic, lo);
        await db.SaveChangesAsync();

        var studentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Create attempts with raw question result for LO
        for (int i = 1; i <= 2; i++)
        {
            var exam = new Exam { InstitutionId = inst.Id, Title = $"Deneme {i}", ExamTypeId = examType.Id, ExamDate = now.AddDays(-5 + i) };
            db.Add(exam);
            await db.SaveChangesAsync();

            var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, DisplayOrder = 1, QuestionCount = 5 };
            db.Add(section);
            await db.SaveChangesAsync();

            var attempt = new StudentExamAttempt { InstitutionId = inst.Id, ExamId = exam.Id, StudentId = studentId, TakenAt = exam.ExamDate, Status = AttemptStatus.Finalized, IsRawAvailable = true };
            db.Add(attempt);
            await db.SaveChangesAsync();

            for (int q = 1; q <= 2; q++)
            {
                var question = new EgitimPlatform.Modules.Questions.Entities.Question { SubjectId = subject.Id, TopicId = topic.Id, SubTopicId = subTopic.Id, LearningOutcomeId = lo.Id, Content = $"Türev sorusu {i}-{q}" };
                db.Add(question);
                await db.SaveChangesAsync();

                var qr = new StudentExamQuestionResult { StudentExamAttemptId = attempt.Id, QuestionId = question.Id, IsCorrect = false, IsBlank = false };
                db.Add(qr);
                await db.SaveChangesAsync();
            }
        }

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);
        var recHandler = new GetStudentAcademicRecommendationHandler(resourceAccess, mapHandler);

        var query = new AcademicRecommendationQuery(studentId, ExamTypeId: examType.Id);
        var recs = await recHandler.GetRecommendationsAsync(query);

        Assert.NotEmpty(recs);
        // Direct LearningOutcome candidate should be recommended and suppress direct parent Topic candidate from duplicating
        var topRec = recs[0];
        Assert.Equal(RecommendationNodeType.LearningOutcome, topRec.NodeType);
        Assert.Equal(lo.Id, topRec.NodeId);
        Assert.Equal(AcademicRecommendationType.RemediateCritical, topRec.RecommendationType);
        Assert.Equal("v1", topRec.RecommendationPolicyVersion);
    }
}
