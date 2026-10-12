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

public class Sprint8SecurityTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Parent_LinkedStudent_ReadAllowed()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var parentUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(parentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Parent).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));
        studentDirectory.IsParentRelatedToStudentAsync(parentUserId, studentId, instId, Arg.Any<CancellationToken>())
            .Returns(true);

        var activePlan = new StudyPlan
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            StudentId = studentId,
            Title = "Active Plan",
            Status = StudyPlanStatus.Active,
            Source = StudyPlanSource.AcademicIntelligence,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(7),
            PlanDays = 7,
            DailyStudyMinutes = 60,
            SessionMinutes = 30,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Set<StudyPlan>().Add(activePlan);
        await db.SaveChangesAsync();

        var getActiveHandler = new GetActiveStudyPlanHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        var activePlanDto = await getActiveHandler.HandleAsync(studentId);
        Assert.NotNull(activePlanDto);

        var listPlansHandler = new ListStudyPlansHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        var plans = await listPlansHandler.HandleAsync(studentId);
        Assert.Single(plans);

        var listCoachTasksHandler = new ListCoachTasksHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        var coachTasks = await listCoachTasksHandler.HandleAsync(studentId);
        Assert.NotNull(coachTasks);
    }

    [Fact]
    public async Task Parent_UnlinkedStudent_ReadForbidden()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var parentUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(parentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Parent).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));
        studentDirectory.IsParentRelatedToStudentAsync(parentUserId, studentId, instId, Arg.Any<CancellationToken>())
            .Returns(false); // Unlinked parent

        var getActiveHandler = new GetActiveStudyPlanHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        await Assert.ThrowsAsync<ForbiddenException>(() => getActiveHandler.HandleAsync(studentId));

        var listCoachTasksHandler = new ListCoachTasksHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        await Assert.ThrowsAsync<ForbiddenException>(() => listCoachTasksHandler.HandleAsync(studentId));
    }

    [Fact]
    public async Task Parent_WriteOperations_Forbidden()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var parentUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(parentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Parent).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));
        studentDirectory.IsParentRelatedToStudentAsync(parentUserId, studentId, instId, Arg.Any<CancellationToken>())
            .Returns(true);

        var mockStudyPlanHandler = Substitute.For<GetStudentAcademicStudyPlanHandler>((IExamResourceAccess)null!, (GetStudentAcademicRecommendationHandler)null!);

        var acceptHandler = new AcceptAcademicStudyPlanHandler(db, currentUser, studentDirectory, mockStudyPlanHandler);
        await Assert.ThrowsAsync<ForbiddenException>(() => acceptHandler.HandleAsync(new AcceptAcademicStudyPlanCommand(StudentId: studentId)));

        var updateTaskHandler = new UpdateTaskCompletionHandler(db, currentUser, studentDirectory);
        await Assert.ThrowsAsync<ForbiddenException>(() => updateTaskHandler.HandleAsync(studentId, Guid.NewGuid(), Guid.NewGuid(), new UpdateTaskCompletionCommand(true)));

        var cancelPlanHandler = new CancelStudyPlanHandler(db, currentUser, studentDirectory);
        await Assert.ThrowsAsync<ForbiddenException>(() => cancelPlanHandler.HandleAsync(studentId, Guid.NewGuid()));

        var createCoachTaskHandler = new CreateCoachTaskHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>(), Substitute.For<IAcademicCatalog>());
        await Assert.ThrowsAsync<ForbiddenException>(() => createCoachTaskHandler.HandleAsync(studentId, new CreateCoachTaskCommand("Parent Coach Task")));

        var updateCoachTaskHandler = new UpdateCoachTaskHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>(), Substitute.For<IAcademicCatalog>());
        await Assert.ThrowsAsync<ForbiddenException>(() => updateCoachTaskHandler.HandleAsync(studentId, Guid.NewGuid(), new UpdateCoachTaskCommand("Title")));

        var cancelCoachTaskHandler = new CancelCoachTaskHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        await Assert.ThrowsAsync<ForbiddenException>(() => cancelCoachTaskHandler.HandleAsync(studentId, Guid.NewGuid()));

        var updateCoachTaskCompletionHandler = new UpdateCoachTaskCompletionHandler(db, currentUser, studentDirectory);
        await Assert.ThrowsAsync<ForbiddenException>(() => updateCoachTaskCompletionHandler.HandleAsync(studentId, Guid.NewGuid(), new UpdateCoachTaskCompletionCommand(true)));
    }

    [Fact]
    public async Task Teacher_UnauthorizedScope_Forbidden()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(teacherUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Teacher).Returns(true);
        currentUser.IsInRole(Roles.InstitutionAdmin).Returns(false);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var getActiveHandler = new GetActiveStudyPlanHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        await Assert.ThrowsAsync<ForbiddenException>(() => getActiveHandler.HandleAsync(studentId));

        var listCoachTasksHandler = new ListCoachTasksHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        await Assert.ThrowsAsync<ForbiddenException>(() => listCoachTasksHandler.HandleAsync(studentId));
    }
}
