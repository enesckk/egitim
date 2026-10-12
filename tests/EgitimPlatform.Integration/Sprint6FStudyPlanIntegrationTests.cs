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

namespace EgitimPlatform.Integration;

public class Sprint6FStudyPlanIntegrationTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetStudyPlan_FullPipelineIntegration_GeneratesValidProposalFromAttempts()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst StudyPlan Test" };
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

        // Seed 2 attempt runs with 2 questions each (total 4 questions for LO) -> satisfies LOMinQuestions (3)
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
        var studyPlanHandler = new GetStudentAcademicStudyPlanHandler(resourceAccess, recHandler);

        var query = new AcademicStudyPlanQuery(
            studentId,
            ExamTypeId: examType.Id,
            PlanDays: 7,
            DailyStudyMinutes: 120,
            StartDate: now.Date
        );

        var plan = await studyPlanHandler.GetStudyPlanAsync(query);

        Assert.NotNull(plan);
        Assert.Equal(studentId, plan.StudentId);
        Assert.Equal(7, plan.PlanDays);
        Assert.Equal(120, plan.DailyStudyMinutes);
        Assert.Equal(30, plan.SessionMinutes);
        Assert.Equal(840, plan.TotalAvailableMinutes);
        Assert.True(plan.TotalPlannedMinutes > 0);
        Assert.Equal(plan.TotalAvailableMinutes - plan.TotalPlannedMinutes, plan.UnallocatedMinutes);
        Assert.True(plan.UtilizationRate > 0m);

        Assert.True(plan.ScheduledRecommendationCount >= 1);
        Assert.Equal(7, plan.Days.Count);

        var scheduledSession = plan.Days.SelectMany(d => d.Sessions).FirstOrDefault(s => s.NodeId == lo.Id);
        Assert.NotNull(scheduledSession);
        Assert.Equal(AcademicRecommendationType.RemediateCritical, scheduledSession.RecommendationType);
        Assert.Equal(RecommendationNodeType.LearningOutcome, scheduledSession.NodeType);
        Assert.Equal(lo.Code, scheduledSession.Code);
        Assert.Equal(lo.Name, scheduledSession.Name);
        Assert.Equal(subject.Id, scheduledSession.SubjectId);
        Assert.Equal(topic.Id, scheduledSession.TopicId);
    }
}
