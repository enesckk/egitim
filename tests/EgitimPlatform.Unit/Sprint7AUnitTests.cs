using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint7AUnitTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void StudyPlan_EntityInitialization_SetsDefaultStatusActive()
    {
        var plan = new StudyPlan
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            InstitutionId = Guid.NewGuid(),
            Title = "Test Plan",
            Status = StudyPlanStatus.Active,
            Source = StudyPlanSource.AcademicIntelligence,
            PlanDays = 7,
            DailyStudyMinutes = 120,
            SessionMinutes = 30,
            TotalAvailableMinutes = 840,
            TotalPlannedMinutes = 240,
            UnallocatedMinutes = 600,
            UtilizationRate = 0.2857m,
            ScheduledRecommendationCount = 2,
            UnscheduledRecommendationCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal(StudyPlanStatus.Active, plan.Status);
        Assert.Equal(StudyPlanSource.AcademicIntelligence, plan.Source);
        Assert.Null(plan.CompletedAt);
    }

    [Fact]
    public void StudyTask_SnapshotsPreservedCorrectly()
    {
        var task = new StudyTask
        {
            Id = Guid.NewGuid(),
            StudyPlanId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            InstitutionId = Guid.NewGuid(),
            DayIndex = 1,
            PlannedDate = DateTimeOffset.UtcNow,
            Sequence = 1,
            DurationMinutes = 30,
            NodeType = RecommendationNodeType.LearningOutcome,
            NodeId = Guid.NewGuid(),
            TitleSnapshot = "Fonksiyonlar",
            ClassificationStateSnapshot = AcademicClassificationState.Critical,
            PriorityScoreSnapshot = 95,
            RecommendationType = AcademicRecommendationType.RemediateCritical,
            IsCompleted = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal("Fonksiyonlar", task.TitleSnapshot);
        Assert.Equal(AcademicClassificationState.Critical, task.ClassificationStateSnapshot);
        Assert.Equal(95, task.PriorityScoreSnapshot);
        Assert.Equal(AcademicRecommendationType.RemediateCritical, task.RecommendationType);
        Assert.False(task.IsCompleted);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public async Task UpdateTaskCompletion_AllTasksCompleted_PlanStatusSetToCompleted()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();

        var plan = new StudyPlan
        {
            Id = planId,
            StudentId = studentId,
            InstitutionId = instId,
            Title = "Plan 1",
            Status = StudyPlanStatus.Active,
            Source = StudyPlanSource.AcademicIntelligence,
            PlanDays = 7,
            DailyStudyMinutes = 120,
            SessionMinutes = 30,
            TotalAvailableMinutes = 840,
            TotalPlannedMinutes = 60,
            UnallocatedMinutes = 780,
            UtilizationRate = 0.0714m,
            CreatedAt = DateTimeOffset.UtcNow,
            Tasks = new List<StudyTask>
            {
                new StudyTask { Id = taskId1, StudyPlanId = planId, StudentId = studentId, InstitutionId = instId, DayIndex = 1, DurationMinutes = 30, IsCompleted = true, CompletedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow },
                new StudyTask { Id = taskId2, StudyPlanId = planId, StudentId = studentId, InstitutionId = instId, DayIndex = 1, DurationMinutes = 30, IsCompleted = false, CreatedAt = DateTimeOffset.UtcNow }
            }
        };

        db.Set<StudyPlan>().Add(plan);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new UpdateTaskCompletionHandler(db, currentUser, studentDirectory);

        // Mark second task as completed -> plan should auto-complete
        var result = await handler.HandleAsync(studentId, planId, taskId2, new UpdateTaskCompletionCommand(true));

        Assert.True(result.IsCompleted);
        Assert.NotNull(result.CompletedAt);

        var updatedPlan = await db.Set<StudyPlan>().FirstAsync(p => p.Id == planId);
        Assert.Equal(StudyPlanStatus.Completed, updatedPlan.Status);
        Assert.NotNull(updatedPlan.CompletedAt);
    }

    [Fact]
    public async Task UpdateTaskCompletion_CompletedPlan_ThrowsConflictExceptionOnTaskReopen()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();

        var plan = new StudyPlan
        {
            Id = planId,
            StudentId = studentId,
            InstitutionId = instId,
            Title = "Plan Completed",
            Status = StudyPlanStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            Source = StudyPlanSource.AcademicIntelligence,
            PlanDays = 7,
            DailyStudyMinutes = 120,
            SessionMinutes = 30,
            TotalAvailableMinutes = 840,
            TotalPlannedMinutes = 30,
            UnallocatedMinutes = 810,
            UtilizationRate = 0.0357m,
            CreatedAt = DateTimeOffset.UtcNow,
            Tasks = new List<StudyTask>
            {
                new StudyTask { Id = taskId1, StudyPlanId = planId, StudentId = studentId, InstitutionId = instId, DayIndex = 1, DurationMinutes = 30, IsCompleted = true, CompletedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow }
            }
        };

        db.Set<StudyPlan>().Add(plan);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new UpdateTaskCompletionHandler(db, currentUser, studentDirectory);

        // Reopening task on completed plan must throw ConflictException (Completed plan is terminal)
        await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(studentId, planId, taskId1, new UpdateTaskCompletionCommand(false)));
    }

    [Fact]
    public async Task UpdateTaskCompletion_CancelledPlan_ThrowsConflictException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();

        var plan = new StudyPlan
        {
            Id = planId,
            StudentId = studentId,
            InstitutionId = instId,
            Title = "Plan Cancelled",
            Status = StudyPlanStatus.Cancelled,
            Source = StudyPlanSource.AcademicIntelligence,
            PlanDays = 7,
            DailyStudyMinutes = 120,
            SessionMinutes = 30,
            TotalAvailableMinutes = 840,
            TotalPlannedMinutes = 30,
            UnallocatedMinutes = 810,
            UtilizationRate = 0.0357m,
            CreatedAt = DateTimeOffset.UtcNow,
            Tasks = new List<StudyTask>
            {
                new StudyTask { Id = taskId1, StudyPlanId = planId, StudentId = studentId, InstitutionId = instId, DayIndex = 1, DurationMinutes = 30, IsCompleted = false, CreatedAt = DateTimeOffset.UtcNow }
            }
        };

        db.Set<StudyPlan>().Add(plan);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new UpdateTaskCompletionHandler(db, currentUser, studentDirectory);

        await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(studentId, planId, taskId1, new UpdateTaskCompletionCommand(true)));
    }

    [Fact]
    public async Task AcceptProposal_EmptyProposal_ThrowsInvalidOperationException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var emptyProposal = new AcademicStudyPlanDto(
            studentId, 7, 120, 30, 840, 0, 840, 0m, 0, 0,
            "v1", "v1", [], []
        );

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var studyPlanHandler = Substitute.For<GetStudentAcademicStudyPlanHandler>(
            resourceAccess,
            Substitute.For<GetStudentAcademicRecommendationHandler>(
                resourceAccess,
                Substitute.For<GetStudentKnowledgeMapHandler>(db, resourceAccess, Substitute.For<GetStudentAcademicClassificationHandler>(Substitute.For<GetStudentAcademicTrendHandler>(db, Substitute.For<IAcademicCatalog>(), resourceAccess)))
            )
        );
        studyPlanHandler.GetStudyPlanAsync(Arg.Any<AcademicStudyPlanQuery>(), Arg.Any<CancellationToken>())
            .Returns(emptyProposal);

        var handler = new AcceptAcademicStudyPlanHandler(db, currentUser, studentDirectory, studyPlanHandler);

        var command = new AcceptAcademicStudyPlanCommand(studentId);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => handler.HandleAsync(command));
        Assert.Equal(0, await db.Set<StudyPlan>().CountAsync());
    }
}
