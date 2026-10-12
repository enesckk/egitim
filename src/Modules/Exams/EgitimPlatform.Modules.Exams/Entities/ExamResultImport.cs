using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Features.ExamResultImport;

namespace EgitimPlatform.Modules.Exams.Entities;

public enum ImportStatus
{
    Completed = 1,
    Failed = 2
}

/// <summary>
/// Sprint 5B — Audit ledger entity for completed exam result import sessions.
/// Guarantees idempotency via unique index on (InstitutionId, ExamId, ImportType, FileHash).
/// </summary>
public class ExamResultImport : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid ExamId { get; set; }
    public ImportType ImportType { get; set; }

    /// <summary>Deterministic SHA-256 hash of the imported workbook bytes.</summary>
    public string FileHash { get; set; } = string.Empty;

    public int TemplateVersion { get; set; } = 1;
    public int TotalRows { get; set; }
    public int ImportedStudentsCount { get; set; }
    public int CreatedAttemptsCount { get; set; }

    public Guid ImportedByUserId { get; set; }
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;
    public ImportStatus Status { get; set; } = ImportStatus.Completed;
}
