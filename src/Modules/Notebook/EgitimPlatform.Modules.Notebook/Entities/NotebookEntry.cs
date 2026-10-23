using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Notebook.Entities;

public class NotebookEntry : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid StudentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public Guid? TopicId { get; set; }
    public string? Tags { get; set; }
    public bool IsPinned { get; set; }
}
