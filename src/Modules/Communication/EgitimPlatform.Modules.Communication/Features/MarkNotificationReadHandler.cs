using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Communication.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Communication.Features;

public class MarkNotificationReadHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(Guid notificationId, CancellationToken ct = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authenticated user identity is required.");
        }

        var userId = currentUser.UserId.Value;

        var notification = await db.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted, ct)
            ?? throw new NotFoundException("Notification not found.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
