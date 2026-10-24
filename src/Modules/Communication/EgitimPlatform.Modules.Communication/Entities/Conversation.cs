using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Communication.Entities;

public class Conversation : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }

    /// <summary>
    /// First participant User ID (ordered so Participant1Id < Participant2Id to guarantee 1 unique 1-on-1 thread).
    /// </summary>
    public Guid Participant1Id { get; set; }

    /// <summary>
    /// Second participant User ID.
    /// </summary>
    public Guid Participant2Id { get; set; }

    public string? Subject { get; set; }

    public DateTimeOffset LastMessageAt { get; set; } = DateTimeOffset.UtcNow;

    public string? LastMessageSnippet { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = [];
}
