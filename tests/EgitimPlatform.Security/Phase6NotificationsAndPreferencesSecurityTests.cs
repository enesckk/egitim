using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Communication.Entities;
using EgitimPlatform.Modules.Communication.Features.ManagePreferences;
using EgitimPlatform.Modules.Communication.Services;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Security;

public class Phase6NotificationsAndPreferencesSecurityTests
{
    private readonly Guid _instA = Guid.NewGuid();
    private readonly Guid _instB = Guid.NewGuid();

    private ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private ICurrentUser CreateUser(bool isAuthenticated, Guid? userId = null, Guid? instId = null, string[]? roles = null)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(isAuthenticated);
        user.UserId.Returns(userId);
        user.Roles.Returns(roles ?? []);
        user.GetInstitutionIdAsync().Returns(Task.FromResult(instId));
        return user;
    }

    [Fact]
    public async Task Notification_Respects_User_Disabled_Preference()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();

        // User opted out of meeting notifications
        db.Set<UserNotificationPreference>().Add(new UserNotificationPreference
        {
            Id = Guid.NewGuid(),
            InstitutionId = _instA,
            UserId = userId,
            InAppEnabled = true,
            MeetingsEnabled = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var notifService = new NotificationService(db);
        await notifService.CreateNotificationAsync(
            _instA,
            userId,
            NotificationType.CoachMeetingScheduled,
            "Toplantı",
            "Toplantı detayı",
            "CoachMeeting",
            Guid.NewGuid());

        var notifs = await db.Set<Notification>().Where(n => n.UserId == userId).ToListAsync();
        Assert.Empty(notifs);
    }

    [Fact]
    public async Task Notification_Deduplicates_Same_RelatedEntity()
    {
        var db = CreateDb();
        var userId = Guid.NewGuid();
        var relatedEntityId = Guid.NewGuid();

        var notifService = new NotificationService(db);
        await notifService.CreateNotificationAsync(
            _instA,
            userId,
            NotificationType.ExamResultPublished,
            "Deneme 1 Sonucu",
            "Sonuçlar açıklandı",
            "ExamAttempt",
            relatedEntityId);

        await notifService.CreateNotificationAsync(
            _instA,
            userId,
            NotificationType.ExamResultPublished,
            "Deneme 1 Sonucu Tekrar",
            "Sonuçlar açıklandı",
            "ExamAttempt",
            relatedEntityId);

        var notifs = await db.Set<Notification>().Where(n => n.UserId == userId).ToListAsync();
        Assert.Single(notifs);
    }

    [Fact]
    public async Task Unauthenticated_User_Cannot_Access_Preferences_FailClosed()
    {
        var db = CreateDb();
        var unauthenticatedUser = CreateUser(isAuthenticated: false);
        var handler = new NotificationPreferencesHandler(db, unauthenticatedUser);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.GetPreferencesAsync());
    }

    [Fact]
    public async Task CrossTenant_User_Preferences_Are_Isolated()
    {
        var db = CreateDb();
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        var userA = CreateUser(true, userAId, _instA, [Roles.Student]);
        var handlerA = new NotificationPreferencesHandler(db, userA);

        await handlerA.UpdatePreferencesAsync(new UpdateNotificationPreferencesRequest(
            InAppEnabled: true,
            ExamResultsEnabled: false,
            CoachTasksEnabled: true,
            MeetingsEnabled: true,
            MessagesEnabled: true,
            MaterialsEnabled: true
        ));

        var userB = CreateUser(true, userBId, _instB, [Roles.Student]);
        var handlerB = new NotificationPreferencesHandler(db, userB);

        var prefB = await handlerB.GetPreferencesAsync();
        // userB should have defaults (ExamResultsEnabled = true)
        Assert.True(prefB.ExamResultsEnabled);
    }
}
