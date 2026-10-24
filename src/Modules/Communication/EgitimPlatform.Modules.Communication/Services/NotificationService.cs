using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Communication.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Communication.Services;

public class NotificationService(IApplicationDbContext db) : INotificationService
{
    public async Task CreateNotificationAsync(
        Guid institutionId,
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken ct = default)
    {
        if (relatedEntityId.HasValue)
        {
            var exists = await db.Set<Notification>()
                .AnyAsync(n => n.UserId == userId
                            && n.Type == type
                            && n.RelatedEntityId == relatedEntityId.Value
                            && !n.IsDeleted, ct);

            if (exists) return;
        }

        // Verify recipient user preference
        var pref = await db.Set<UserNotificationPreference>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.InstitutionId == institutionId && p.UserId == userId && !p.IsDeleted, ct);

        if (pref != null)
        {
            if (!pref.InAppEnabled) return;
            if (type == NotificationType.ExamResultPublished && !pref.ExamResultsEnabled) return;
            if ((type == NotificationType.CoachTaskAssigned || type == NotificationType.CoachTaskDueSoon) && !pref.CoachTasksEnabled) return;
            if (type == NotificationType.CoachMeetingScheduled && !pref.MeetingsEnabled) return;
            if (type == NotificationType.NewMessageReceived && !pref.MessagesEnabled) return;
            if (type == NotificationType.ContentMaterialShared && !pref.MaterialsEnabled) return;
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            ReadAt = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Set<Notification>().Add(notification);
        await db.SaveChangesAsync(ct);
    }
}
