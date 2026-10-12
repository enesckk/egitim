using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Communication.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Communication.Features;

public class ListNotificationsHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
{
    public async Task<List<NotificationDto>> HandleAsync(bool? isRead = null, CancellationToken ct = default)
    {
        if (!currentUser.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Authenticated user identity is required.");
        }

        var userId = currentUser.UserId.Value;

        var query = db.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDeleted);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var list = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return list.Select(n => new NotificationDto(
            n.Id,
            n.InstitutionId,
            n.UserId,
            n.Type,
            n.Title,
            n.Message,
            n.RelatedEntityType,
            n.RelatedEntityId,
            n.IsRead,
            n.ReadAt,
            n.CreatedAt
        )).ToList();
    }
}
