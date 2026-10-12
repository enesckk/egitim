using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Communication.Entities;
using EgitimPlatform.Modules.Communication.Features;
using EgitimPlatform.Modules.Communication.Jobs;
using EgitimPlatform.Modules.Communication.Services;
using EgitimPlatform.Modules.Planning.Entities;
using EgitimPlatform.Modules.Planning.Features;

using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Integration;

public class Sprint10IntegrationTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task CoachTaskCreation_TriggersNotification()
    {
        using var db = Db();
        var instId = Guid.NewGuid();
        var coachUserId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        db.Set<EgitimPlatform.Modules.Students.Entities.Student>().Add(new EgitimPlatform.Modules.Students.Entities.Student
        {
            Id = studentId,
            InstitutionId = instId,
            UserId = studentUserId,
            FirstName = "Deniz",
            LastName = "Kaya",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(coachUserId);
        currentUser.GetInstitutionIdAsync().Returns(instId);
        currentUser.IsInRole(Roles.Coach).Returns(true);

        var studentDirectory = Substitute.For<IStudentDirectory>();
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, studentUserId));

        var coachQuery = Substitute.For<ICoachStudentQuery>();
        coachQuery.HasActiveAssignmentAsync(coachUserId, instId, studentId, Arg.Any<CancellationToken>())
            .Returns(true);

        var notificationService = new NotificationService(db);

        var createCoachTaskHandler = new CreateCoachTaskHandler(
            db, currentUser, studentDirectory, coachQuery, Substitute.For<IAcademicCatalog>(), notificationService);

        var dto = await createCoachTaskHandler.HandleAsync(studentId, new CreateCoachTaskCommand("Integrasyon Görevi"));
        Assert.NotNull(dto);

        var notification = await db.Set<Notification>()
            .FirstOrDefaultAsync(n => n.UserId == studentUserId && n.RelatedEntityId == dto.Id);
        Assert.NotNull(notification);
        Assert.Equal(NotificationType.CoachTaskAssigned, notification.Type);
    }

    [Fact]
    public async Task DueSoonNotificationJob_ScansTasks_CreatesNotifications()
    {
        using var db = Db();
        var instId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        db.Set<EgitimPlatform.Modules.Students.Entities.Student>().Add(new EgitimPlatform.Modules.Students.Entities.Student
        {
            Id = studentId,
            InstitutionId = instId,
            UserId = studentUserId,
            FirstName = "Can",
            LastName = "Oz",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var coachTask = new CoachTask
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            StudentId = studentId,
            CoachUserId = Guid.NewGuid(),
            Title = "Yarın Teslim Görev",
            DueDate = DateTimeOffset.UtcNow.AddHours(12),
            Status = CoachTaskStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Set<CoachTask>().Add(coachTask);
        await db.SaveChangesAsync();

        var notificationService = new NotificationService(db);
        var job = new DueSoonNotificationJob(db, notificationService);
        await job.ExecuteAsync();

        var notification = await db.Set<Notification>()
            .FirstOrDefaultAsync(n => n.UserId == studentUserId && n.RelatedEntityId == coachTask.Id);
        Assert.NotNull(notification);
        Assert.Equal(NotificationType.CoachTaskDueSoon, notification.Type);
    }
}
