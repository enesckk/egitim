using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Integration;

public class Sprint7AIntegrationTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task AcceptProposal_FullIntegration_PersistsPlanAndTasksAtomically()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Sprint 7A Test" };
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
        var studentUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Seed 2 exam attempts with incorrect answers to produce a Critical recommendation
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
                var question = new EgitimPlatform.Modules.Questions.Entities.Question { SubjectId = subject.Id, TopicId = topic.Id, SubTopicId = subTopic.Id, LearningOutcomeId = lo.Id, Content = $"Soru {i}-{q}" };
                db.Add(question);
                await db.SaveChangesAsync();

                var qr = new StudentExamQuestionResult { StudentExamAttemptId = attempt.Id, QuestionId = question.Id, IsCorrect = false, IsBlank = false };
                db.Add(qr);
                await db.SaveChangesAsync();
            }
        }

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateStudentReadAccessAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, inst.Id, studentUserId));

        var trendHandler = new GetStudentAcademicTrendHandler(db, Substitute.For<IAcademicCatalog>(), resourceAccess);
        var classificationHandler = new GetStudentAcademicClassificationHandler(trendHandler);
        var mapHandler = new GetStudentKnowledgeMapHandler(db, resourceAccess, classificationHandler);
        var recHandler = new GetStudentAcademicRecommendationHandler(resourceAccess, mapHandler);
        var studyPlanHandler = new GetStudentAcademicStudyPlanHandler(resourceAccess, recHandler);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, inst.Id, studentUserId));

        var acceptHandler = new AcceptAcademicStudyPlanHandler(db, currentUser, studentDirectory, studyPlanHandler);

        var command = new AcceptAcademicStudyPlanCommand(
            StudentId: studentId,
            ExamTypeId: examType.Id,
            PlanDays: 7,
            DailyStudyMinutes: 120
        );

        var planDetail = await acceptHandler.HandleAsync(command);

        Assert.NotNull(planDetail);
        Assert.Equal(studentId, planDetail.Plan.StudentId);
        Assert.Equal(inst.Id, planDetail.Plan.InstitutionId);
        Assert.Equal(StudyPlanStatus.Active, planDetail.Plan.Status);
        Assert.True(planDetail.Tasks.Count > 0);

        // Verify DB Persistence
        var persistedPlan = await db.Set<StudyPlan>().Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == planDetail.Plan.Id);
        Assert.NotNull(persistedPlan);
        Assert.Equal(planDetail.Tasks.Count, persistedPlan.Tasks.Count);
        Assert.All(persistedPlan.Tasks, t => Assert.False(t.IsCompleted));

        // Test Active Plan Uniqueness Conflict
        await Assert.ThrowsAsync<ConflictException>(() => acceptHandler.HandleAsync(command));

        // Test GetActiveStudyPlanHandler
        var activeHandler = new GetActiveStudyPlanHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        var activePlan = await activeHandler.HandleAsync(studentId);
        Assert.NotNull(activePlan);
        Assert.Equal(planDetail.Plan.Id, activePlan.Plan.Id);
    }

    [Fact]
    public async Task TaskCompletion_TerminalPlanLifecycle_CompletedPlanCannotBeReopened()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, studentUserId));

        var planA = new StudyPlan
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            InstitutionId = instId,
            Title = "Plan A",
            Status = StudyPlanStatus.Active,
            Source = StudyPlanSource.AcademicIntelligence,
            PlanDays = 7,
            DailyStudyMinutes = 120,
            SessionMinutes = 30,
            TotalAvailableMinutes = 840,
            TotalPlannedMinutes = 30,
            UnallocatedMinutes = 810,
            UtilizationRate = 0.0357m,
            CreatedAt = DateTimeOffset.UtcNow,
            Tasks = new List<StudyTask>()
        };
        var taskA = new StudyTask
        {
            Id = Guid.NewGuid(),
            StudyPlanId = planA.Id,
            StudentId = studentId,
            InstitutionId = instId,
            DayIndex = 1,
            DurationMinutes = 30,
            IsCompleted = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        planA.Tasks.Add(taskA);

        db.Set<StudyPlan>().Add(planA);
        await db.SaveChangesAsync();

        var updateHandler = new UpdateTaskCompletionHandler(db, currentUser, studentDirectory);

        // Complete task -> Plan A status becomes Completed
        await updateHandler.HandleAsync(studentId, planA.Id, taskA.Id, new UpdateTaskCompletionCommand(true));
        var completedPlanA = await db.Set<StudyPlan>().FirstAsync(p => p.Id == planA.Id);
        Assert.Equal(StudyPlanStatus.Completed, completedPlanA.Status);

        // Add new Plan B as Active
        var planB = new StudyPlan
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            InstitutionId = instId,
            Title = "Plan B",
            Status = StudyPlanStatus.Active,
            Source = StudyPlanSource.AcademicIntelligence,
            PlanDays = 7,
            DailyStudyMinutes = 120,
            SessionMinutes = 30,
            TotalAvailableMinutes = 840,
            TotalPlannedMinutes = 30,
            UnallocatedMinutes = 810,
            UtilizationRate = 0.0357m,
            CreatedAt = DateTimeOffset.UtcNow,
            Tasks = new List<StudyTask>()
        };
        db.Set<StudyPlan>().Add(planB);
        await db.SaveChangesAsync();

        // Attempting to reopen task on Completed Plan A must throw ConflictException and NOT reactivate Plan A
        await Assert.ThrowsAsync<ConflictException>(() => updateHandler.HandleAsync(studentId, planA.Id, taskA.Id, new UpdateTaskCompletionCommand(false)));

        Assert.Equal(StudyPlanStatus.Completed, (await db.Set<StudyPlan>().FirstAsync(p => p.Id == planA.Id)).Status);
        Assert.Equal(StudyPlanStatus.Active, (await db.Set<StudyPlan>().FirstAsync(p => p.Id == planB.Id)).Status);
    }
}
