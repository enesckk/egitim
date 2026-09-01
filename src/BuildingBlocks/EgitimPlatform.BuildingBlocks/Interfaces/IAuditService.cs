namespace EgitimPlatform.BuildingBlocks.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        Guid userId,
        string action,
        string entityType,
        string entityId,
        Guid? institutionId = null,
        string? metadataJson = null,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}
