using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Communication.Entities;

public class UserNotificationPreference : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid UserId { get; set; }

    public bool InAppEnabled { get; set; } = true;
    public bool ExamResultsEnabled { get; set; } = true;
    public bool CoachTasksEnabled { get; set; } = true;
    public bool MeetingsEnabled { get; set; } = true;
    public bool MessagesEnabled { get; set; } = true;
    public bool MaterialsEnabled { get; set; } = true;
}
