using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Communication.Entities;

public class Message : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }

    public Guid ConversationId { get; set; }

    public virtual Conversation? Conversation { get; set; }

    public Guid SenderUserId { get; set; }

    public Guid RecipientUserId { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
}
