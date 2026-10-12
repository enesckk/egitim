using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Communication.Entities;
using EgitimPlatform.Modules.Communication.Features;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Sprint10SecurityTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task OwnNotifications_Allowed()
    {
        using var db = Db();
        var user1Id = Guid.NewGuid();
        var instId = Guid.NewGuid();

        db.Set<Notification>().Add(new Notification
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            UserId = user1Id,
            Type = NotificationType.CoachTaskAssigned,
            Title = "Gorev 1",
            Message = "Mesaj 1",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(user1Id);

        var listHandler = new ListNotificationsHandler(db, currentUser);
        var list = await listHandler.HandleAsync();
        Assert.Single(list);
        Assert.Equal(user1Id, list[0].UserId);
    }

    [Fact]
    public async Task CrossUser_NotificationAccess_ForbiddenOrNotFound()
    {
        using var db = Db();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            InstitutionId = instId,
            UserId = user1Id,
            Type = NotificationType.CoachTaskAssigned,
            Title = "User 1 Gorevi",
            Message = "Mesaj 1",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Set<Notification>().Add(notification);
        await db.SaveChangesAsync();

        var currentUser2 = Substitute.For<ICurrentUser>();
        currentUser2.UserId.Returns(user2Id); // User 2 trying to read User 1's notification

        var listHandler = new ListNotificationsHandler(db, currentUser2);
        var user2List = await listHandler.HandleAsync();
        Assert.Empty(user2List); // Isolated to User 2 (User 1's notification not leaked)

        var markReadHandler = new MarkNotificationReadHandler(db, currentUser2);
        await Assert.ThrowsAsync<NotFoundException>(() => markReadHandler.HandleAsync(notification.Id));
    }

    [Fact]
    public async Task Anonymous_NotificationAccess_ThrowsUnauthorized()
    {
        using var db = Db();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null); // Anonymous

        var listHandler = new ListNotificationsHandler(db, currentUser);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => listHandler.HandleAsync());
    }
}
