using EgitimPlatform.BuildingBlocks.Constants;

namespace EgitimPlatform.BuildingBlocks.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(
        Guid institutionId,
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken ct = default);
}
