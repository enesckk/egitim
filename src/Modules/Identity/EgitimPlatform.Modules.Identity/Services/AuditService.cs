using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Infrastructure;

namespace EgitimPlatform.Modules.Identity.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// P2-02: Adds an audit log entry to the context WITHOUT saving.
    /// Caller is responsible for calling SaveChangesAsync atomically with business changes.
    /// </summary>
    public Task AddPendingLogAsync(
        Guid userId,
        string action,
        string entityType,
        string entityId,
        Guid? institutionId = null,
        string? metadataJson = null,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            InstitutionId = institutionId,
            MetadataJson = metadataJson,
            CorrelationId = correlationId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTimeOffset.UtcNow,
        };

        _context.AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Legacy: adds and saves atomically (own SaveChanges).
    /// Prefer AddPendingLogAsync + caller SaveChanges for cross-entity atomicity.
    /// </summary>
    public async Task LogAsync(
        Guid userId,
        string action,
        string entityType,
        string entityId,
        Guid? institutionId = null,
        string? metadataJson = null,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        await AddPendingLogAsync(userId, action, entityType, entityId, institutionId, metadataJson, correlationId, ipAddress, userAgent);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
