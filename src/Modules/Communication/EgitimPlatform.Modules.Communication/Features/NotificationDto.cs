using EgitimPlatform.BuildingBlocks.Constants;

namespace EgitimPlatform.Modules.Communication.Features;

public record NotificationDto(
    Guid Id,
    Guid InstitutionId,
    Guid UserId,
    NotificationType Type,
    string Title,
    string Message,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt
);
