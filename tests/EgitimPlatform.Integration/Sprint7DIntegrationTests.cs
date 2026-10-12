using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Integration;

public class Sprint7DIntegrationTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task FullCoachTaskLifecycle_Create_StudentRead_StudentComplete_CoachVerify()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, studentUserId));

        var coachQuery = Substitute.For<ICoachStudentQuery>();
        coachQuery.HasActiveAssignmentAsync(coachUserId, instId, studentId, Arg.Any<CancellationToken>())
            .Returns(true);

        var academicCatalog = Substitute.For<IAcademicCatalog>();

        // 1. Coach creates task
        var coachUser = Substitute.For<ICurrentUser>();
        coachUser.UserId.Returns(coachUserId);
        coachUser.GetInstitutionIdAsync().Returns(instId);
        coachUser.IsInRole(Roles.Coach).Returns(true);

        var createHandler = new CreateCoachTaskHandler(db, coachUser, studentDirectory, coachQuery, academicCatalog);
        var createdTask = await createHandler.HandleAsync(studentId, new CreateCoachTaskCommand("Solve 30 Physics Questions", "Focus on Vectors", DateTimeOffset.UtcNow.AddDays(1), 45));

        Assert.NotNull(createdTask);
        Assert.Equal("Solve 30 Physics Questions", createdTask.Title);
        Assert.Equal(CoachTaskStatus.Active, createdTask.Status);

        // 2. Student reads task
        var studentUser = Substitute.For<ICurrentUser>();
        studentUser.UserId.Returns(studentUserId);
        studentUser.GetInstitutionIdAsync().Returns(instId);
        studentUser.IsInRole(Roles.Student).Returns(true);

        var listHandler = new ListCoachTasksHandler(db, studentUser, studentDirectory, coachQuery);
        var studentTasks = await listHandler.HandleAsync(studentId);

        Assert.Single(studentTasks);
        Assert.Equal(createdTask.Id, studentTasks[0].Id);

        // 3. Student completes task
        var completionHandler = new UpdateCoachTaskCompletionHandler(db, studentUser, studentDirectory);
        var completedTask = await completionHandler.HandleAsync(studentId, createdTask.Id, new UpdateCoachTaskCompletionCommand(true));

        Assert.True(completedTask.IsCompleted);
        Assert.Equal(CoachTaskStatus.Completed, completedTask.Status);

        // 4. Coach verifies completion in list
        var coachListHandler = new ListCoachTasksHandler(db, coachUser, studentDirectory, coachQuery);
        var coachTasks = await coachListHandler.HandleAsync(studentId);

        Assert.Single(coachTasks);
        Assert.True(coachTasks[0].IsCompleted);
        Assert.NotNull(coachTasks[0].CompletedAt);
    }

    [Fact]
    public async Task AssignmentRemoval_BlocksCoachFromTaskOperations()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var coachQuery = Substitute.For<ICoachStudentQuery>();
        // Assignment is removed
        coachQuery.HasActiveAssignmentAsync(coachUserId, instId, studentId, Arg.Any<CancellationToken>())
            .Returns(false);

        var coachUser = Substitute.For<ICurrentUser>();
        coachUser.UserId.Returns(coachUserId);
        coachUser.GetInstitutionIdAsync().Returns(instId);
        coachUser.IsInRole(Roles.Coach).Returns(true);

        var createHandler = new CreateCoachTaskHandler(db, coachUser, studentDirectory, coachQuery, Substitute.For<IAcademicCatalog>());
        await Assert.ThrowsAsync<ForbiddenException>(() => createHandler.HandleAsync(studentId, new CreateCoachTaskCommand("Unassigned Task")));
    }
}
