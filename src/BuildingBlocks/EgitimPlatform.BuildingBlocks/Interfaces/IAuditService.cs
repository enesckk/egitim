namespace EgitimPlatform.BuildingBlocks.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Adds an audit log entry to the shared DbContext without saving.
    /// The caller is responsible for the final SaveChangesAsync.
    /// This ensures the audit record is atomic with the business operation.
    /// </summary>
    Task AddPendingLogAsync(
        Guid userId,
        string action,
        string entityType,
        string entityId,
        Guid? institutionId = null,
        string? metadataJson = null,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null);

    /// <summary>
    /// Legacy: adds and saves in one call. Use AddPendingLogAsync for atomic operations.
    /// </summary>
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
