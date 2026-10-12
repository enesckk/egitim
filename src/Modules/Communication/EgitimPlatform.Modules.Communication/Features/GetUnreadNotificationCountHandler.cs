using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Communication.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Communication.Features;

public class GetUnreadNotificationCountHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
{
    public async Task<int> HandleAsync(CancellationToken ct = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authenticated user identity is required.");
        }

        var userId = currentUser.UserId.Value;

        return await db.Set<Notification>()
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted, ct);
    }
}
