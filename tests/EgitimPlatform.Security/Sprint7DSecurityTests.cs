using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Sprint7DSecurityTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CreateTask_UnassignedCoach_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var coachQuery = Substitute.For<ICoachStudentQuery>();
        coachQuery.HasActiveAssignmentAsync(coachUserId, instId, studentId, Arg.Any<CancellationToken>())
            .Returns(false); // Unassigned

        var handler = new CreateCoachTaskHandler(db, currentUser, studentDirectory, coachQuery, Substitute.For<IAcademicCatalog>());
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId, new CreateCoachTaskCommand("Unauthorized Task")));
    }

    [Fact]
    public async Task UpdateTaskContent_StudentRole_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(studentUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Student).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, studentUserId));

        var handler = new UpdateCoachTaskHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>(), Substitute.For<IAcademicCatalog>());
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId, taskId, new UpdateCoachTaskCommand("Student Try Edit")));
    }

    [Fact]
    public async Task CompleteTask_CoachRole_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new UpdateCoachTaskCompletionHandler(db, currentUser, studentDirectory);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId, taskId, new UpdateCoachTaskCompletionCommand(true)));
    }

    [Fact]
    public async Task ListTasks_TeacherRole_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(teacherUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Teacher).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new ListCoachTasksHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>());
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId));
    }

    [Fact]
    public async Task CreateTask_CrossInstitution_ThrowsForbiddenException()
    {
        using var db = Db();
        var studentId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var instId = Guid.NewGuid();
        var otherInstId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(otherInstId); // Cross-tenant boundary
        currentUser.IsInRole(Roles.Coach).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, Guid.NewGuid()));

        var handler = new CreateCoachTaskHandler(db, currentUser, studentDirectory, Substitute.For<ICoachStudentQuery>(), Substitute.For<IAcademicCatalog>());
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(studentId, new CreateCoachTaskCommand("Cross Tenant Task")));
    }
}
