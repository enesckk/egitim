using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Sprint7CCoachSecurityTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetActiveStudyPlan_AssignedCoach_Allowed()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var plan = new StudyPlan
        {
            Id = planId,
            StudentId = studentId,
            InstitutionId = instId,
            Title = "Coach Oversight Plan",
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
                new StudyTask { Id = Guid.NewGuid(), StudyPlanId = planId, StudentId = studentId, InstitutionId = instId, DayIndex = 1, DurationMinutes = 60, IsCompleted = false, CreatedAt = DateTimeOffset.UtcNow }
            }
        };
        db.Set<StudyPlan>().Add(plan);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(false);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);
        currentUser.UserId.Returns(coachUserId);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var coachQuery = Substitute.For<ICoachStudentQuery>();
        coachQuery.HasActiveAssignmentAsync(coachUserId, instId, studentId, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new GetActiveStudyPlanHandler(db, currentUser, studentDirectory, coachQuery);
        var result = await handler.HandleAsync(studentId);

        Assert.NotNull(result);
        Assert.Equal(planId, result.Plan.Id);
    }

    [Fact]
    public async Task GetActiveStudyPlan_UnassignedCoach_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(false);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);
        currentUser.UserId.Returns(coachUserId);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var coachQuery = Substitute.For<ICoachStudentQuery>();
        coachQuery.HasActiveAssignmentAsync(coachUserId, instId, studentId, Arg.Any<CancellationToken>())
            .Returns(false); // Unassigned

        var handler = new GetActiveStudyPlanHandler(db, currentUser, studentDirectory, coachQuery);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId));
    }

    [Fact]
    public async Task GetActiveStudyPlan_TeacherRole_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsSuperAdmin.Returns(false);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Teacher).Returns(true);
        currentUser.UserId.Returns(teacherUserId);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new GetActiveStudyPlanHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId));
    }

    [Fact]
    public async Task AcceptProposal_CoachRole_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsInRole(Roles.Coach).Returns(true);
        currentUser.UserId.Returns(coachUserId);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var studyPlanProposalHandler = Substitute.For<GetStudentAcademicStudyPlanHandler>(
            Substitute.For<IExamResourceAccess>(),
            Substitute.For<GetStudentAcademicRecommendationHandler>(
                Substitute.For<IExamResourceAccess>(),
                Substitute.For<GetStudentKnowledgeMapHandler>(db, Substitute.For<IExamResourceAccess>(), Substitute.For<GetStudentAcademicClassificationHandler>(Substitute.For<GetStudentAcademicTrendHandler>(db, Substitute.For<IAcademicCatalog>(), Substitute.For<IExamResourceAccess>())))
            )
        );

        var handler = new AcceptAcademicStudyPlanHandler(db, currentUser, studentDirectory, studyPlanProposalHandler);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(new AcceptAcademicStudyPlanCommand(studentId)));
    }

    [Fact]
    public async Task UpdateTaskCompletion_CoachRole_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsInRole(Roles.Coach).Returns(true);
        currentUser.UserId.Returns(coachUserId);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new UpdateTaskCompletionHandler(db, currentUser, studentDirectory);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId, planId, taskId, new UpdateTaskCompletionCommand(true)));
    }

    [Fact]
    public async Task CancelStudyPlan_CoachRole_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsInRole(Roles.Coach).Returns(true);
        currentUser.UserId.Returns(coachUserId);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new CancelStudyPlanHandler(db, currentUser, studentDirectory);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId, planId));
    }
}
