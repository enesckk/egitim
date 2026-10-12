using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Communication.Entities;
using EgitimPlatform.Modules.Communication.Features;
using EgitimPlatform.Modules.Communication.Services;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint10UnitTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task NotificationCreation_And_Deduplication()
    {
        using var db = Db();
        var service = new NotificationService(db);
        var instId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var relatedId = Guid.NewGuid();

        // First creation
        await service.CreateNotificationAsync(instId, userId, NotificationType.CoachTaskAssigned, "Title 1", "Msg 1", "CoachTask", relatedId);

        var count = await db.Set<Notification>().CountAsync(n => n.UserId == userId);
        Assert.Equal(1, count);

        // Retry with same (Type, RelatedEntityId, UserId) should deduplicate
        await service.CreateNotificationAsync(instId, userId, NotificationType.CoachTaskAssigned, "Title 1", "Msg 1", "CoachTask", relatedId);

        var countAfterRetry = await db.Set<Notification>().CountAsync(n => n.UserId == userId);
        Assert.Equal(1, countAfterRetry);
    }

    [Fact]
    public async Task MarkNotificationRead_And_MarkAllRead()
    {
        using var db = Db();
        var userId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var n1 = new Notification
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            UserId = userId,
            Type = NotificationType.CoachTaskAssigned,
            Title = "Task 1",
            Message = "Msg 1",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var n2 = new Notification
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            UserId = userId,
            Type = NotificationType.StudyPlanCompleted,
            Title = "Plan 1",
            Message = "Msg 2",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Set<Notification>().AddRange(n1, n2);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(userId);

        var markReadHandler = new MarkNotificationReadHandler(db, currentUser);
        await markReadHandler.HandleAsync(n1.Id);

        var updatedN1 = await db.Set<Notification>().FindAsync(n1.Id);
        Assert.True(updatedN1!.IsRead);
        Assert.NotNull(updatedN1.ReadAt);

        var countHandler = new GetUnreadNotificationCountHandler(db, currentUser);
        var unreadCount = await countHandler.HandleAsync();
        Assert.Equal(1, unreadCount);

        var markAllReadHandler = new MarkAllNotificationsReadHandler(db, currentUser);
        await markAllReadHandler.HandleAsync();

        var finalUnreadCount = await countHandler.HandleAsync();
        Assert.Equal(0, finalUnreadCount);
    }
}
