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
