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

public class Sprint7BUnitTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetActiveStudyPlan_DerivedProgressMetrics_CalculatedCorrectly()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var plan = new StudyPlan
        {
            Id = planId,
            StudentId = studentId,
            InstitutionId = instId,
            Title = "Progress Test Plan",
            Status = StudyPlanStatus.Active,
            Source = StudyPlanSource.AcademicIntelligence,
            PlanDays = 7,
            DailyStudyMinutes = 120,
            SessionMinutes = 30,
            TotalAvailableMinutes = 840,
            TotalPlannedMinutes = 120,
            UnallocatedMinutes = 720,
            UtilizationRate = 0.1429m,
            CreatedAt = DateTimeOffset.UtcNow,
            Tasks = new List<StudyTask>
            {
                new StudyTask { Id = Guid.NewGuid(), StudyPlanId = planId, StudentId = studentId, InstitutionId = instId, DayIndex = 1, DurationMinutes = 30, IsCompleted = true, CompletedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow },
                new StudyTask { Id = Guid.NewGuid(), StudyPlanId = planId, StudentId = studentId, InstitutionId = instId, DayIndex = 1, DurationMinutes = 30, IsCompleted = true, CompletedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow },
                new StudyTask { Id = Guid.NewGuid(), StudyPlanId = planId, StudentId = studentId, InstitutionId = instId, DayIndex = 2, DurationMinutes = 30, IsCompleted = false, CreatedAt = DateTimeOffset.UtcNow },
                new StudyTask { Id = Guid.NewGuid(), StudyPlanId = planId, StudentId = studentId, InstitutionId = instId, DayIndex = 2, DurationMinutes = 30, IsCompleted = false, CreatedAt = DateTimeOffset.UtcNow }
            }
        };

        db.Set<StudyPlan>().Add(plan);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new GetActiveStudyPlanHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        var result = await handler.HandleAsync(studentId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Plan.CompletedTaskCount);
        Assert.Equal(4, result.Plan.TotalTaskCount);
        Assert.Equal(2, result.Plan.RemainingTaskCount);
        Assert.Equal(60, result.Plan.CompletedTaskMinutes);
        Assert.Equal(0.5000m, result.Plan.TaskProgressRate);
        Assert.Equal(0.5000m, result.Plan.MinuteProgressRate);
    }

    [Fact]
    public async Task CancelStudyPlan_ActivePlan_SetsStatusToCancelled()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var plan = new StudyPlan
        {
            Id = planId,
            StudentId = studentId,
            InstitutionId = instId,
            Title = "Plan To Cancel",
            Status = StudyPlanStatus.Active,
            Source = StudyPlanSource.AcademicIntelligence,
            PlanDays = 7,
            DailyStudyMinutes = 120,
            SessionMinutes = 30,
            TotalAvailableMinutes = 840,
            TotalPlannedMinutes = 60,
            UnallocatedMinutes = 780,
            UtilizationRate = 0.0714m,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Set<StudyPlan>().Add(plan);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new CancelStudyPlanHandler(db, currentUser, studentDirectory);
        await handler.HandleAsync(studentId, planId);

        var cancelledPlan = await db.Set<StudyPlan>().FirstAsync(p => p.Id == planId);
        Assert.Equal(StudyPlanStatus.Cancelled, cancelledPlan.Status);
    }
}
