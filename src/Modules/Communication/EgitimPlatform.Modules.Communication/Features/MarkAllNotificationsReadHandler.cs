using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Communication.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Communication.Features;

public class MarkAllNotificationsReadHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(CancellationToken ct = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authenticated user identity is required.");
        }

        var userId = currentUser.UserId.Value;
        var unreadNotifications = await db.Set<Notification>()
            .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
            .ToListAsync(ct);

        if (unreadNotifications.Count == 0) return;

        var now = DateTimeOffset.UtcNow;
        foreach (var n in unreadNotifications)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await db.SaveChangesAsync(ct);
    }
}
