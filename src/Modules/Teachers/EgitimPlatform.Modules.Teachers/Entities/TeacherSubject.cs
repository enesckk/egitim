using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Teachers.Entities;

/// <summary>Teacher subject scope; does not confer access to any student.</summary>
public class TeacherSubject : SoftDeletableEntity, IHasInstitutionId
{
    public Guid TeacherId { get; set; }
    public Guid InstitutionId { get; set; }

    /// <summary>Subject name — will be replaced by FK to Academic.Subject in future sprint.</summary>
    public Guid SubjectId { get; set; }
}
