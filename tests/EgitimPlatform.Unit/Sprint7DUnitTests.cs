using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint7DUnitTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreateCoachTask_DefaultState_ActiveAndNotCompleted()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var coachQuery = Substitute.For<ICoachStudentQuery>();
        coachQuery.HasActiveAssignmentAsync(coachUserId, instId, studentId, Arg.Any<CancellationToken>())
            .Returns(true);

        var academicCatalog = Substitute.For<IAcademicCatalog>();

        var handler = new CreateCoachTaskHandler(db, currentUser, studentDirectory, coachQuery, academicCatalog);
        var result = await handler.HandleAsync(studentId, new CreateCoachTaskCommand("Paragraph Practice", "Solve 20 questions", DateTimeOffset.UtcNow.AddDays(2), 30));

        Assert.NotNull(result);
        Assert.Equal("Paragraph Practice", result.Title);
        Assert.Equal(CoachTaskStatus.Active, result.Status);
        Assert.False(result.IsCompleted);
        Assert.Null(result.CompletedAt);
    }

    [Fact]
    public async Task CompleteCoachTask_StudentTogglesCompletion_SetsStatusAndCompletedAt()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var task = new CoachTask
        {
            Id = taskId,
            StudentId = studentId,
            InstitutionId = instId,
            CoachUserId = Guid.NewGuid(),
            Title = "Solve 20 Math Questions",
            Status = CoachTaskStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Set<CoachTask>().Add(task);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(studentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Student).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, studentUserId));

        var handler = new UpdateCoachTaskCompletionHandler(db, currentUser, studentDirectory);
        var result = await handler.HandleAsync(studentId, taskId, new UpdateCoachTaskCompletionCommand(true));

        Assert.True(result.IsCompleted);
        Assert.Equal(CoachTaskStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedAt);

        // Toggle back to active
        var uncompletedResult = await handler.HandleAsync(studentId, taskId, new UpdateCoachTaskCompletionCommand(false));
        Assert.False(uncompletedResult.IsCompleted);
        Assert.Equal(CoachTaskStatus.Active, uncompletedResult.Status);
        Assert.Null(uncompletedResult.CompletedAt);
    }

    [Fact]
    public async Task UpdateCompletedOrCancelledTask_ThrowsConflictException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var task = new CoachTask
        {
            Id = taskId,
            StudentId = studentId,
            InstitutionId = instId,
            CoachUserId = coachUserId,
            Title = "Finished Task",
            Status = CoachTaskStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Set<CoachTask>().Add(task);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var coachQuery = Substitute.For<ICoachStudentQuery>();
        coachQuery.HasActiveAssignmentAsync(coachUserId, instId, studentId, Arg.Any<CancellationToken>())
            .Returns(true);

        var updateHandler = new UpdateCoachTaskHandler(db, currentUser, studentDirectory, coachQuery, Substitute.For<IAcademicCatalog>());
        await Assert.ThrowsAsync<ConflictException>(() => updateHandler.HandleAsync(studentId, taskId, new UpdateCoachTaskCommand("Try Update")));
    }

    [Fact]
    public async Task CancelTask_ActiveTask_SetsStatusToCancelled()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var task = new CoachTask
        {
            Id = taskId,
            StudentId = studentId,
            InstitutionId = instId,
            CoachUserId = coachUserId,
            Title = "Task To Cancel",
            Status = CoachTaskStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Set<CoachTask>().Add(task);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var coachQuery = Substitute.For<ICoachStudentQuery>();
        coachQuery.HasActiveAssignmentAsync(coachUserId, instId, studentId, Arg.Any<CancellationToken>())
            .Returns(true);

        var cancelHandler = new CancelCoachTaskHandler(db, currentUser, studentDirectory, coachQuery);
        var result = await cancelHandler.HandleAsync(studentId, taskId);

        Assert.Equal(CoachTaskStatus.Cancelled, result.Status);
    }
}
