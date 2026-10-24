using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;

namespace EgitimPlatform.Modules.Coaching.Entities;

public enum CoachMeetingStatus
{
    Upcoming = 1,
    Completed = 2,
    Cancelled = 3
}

public class CoachMeeting : SoftDeletableEntity, IHasInstitutionId
{
    public Guid InstitutionId { get; set; }
    public Guid CoachId { get; set; }
    public Guid StudentId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string MeetingType { get; set; } = "Haftalık Takip & Analiz";
    public string LocationType { get; set; } = "Online Video"; // "Online Video" or "Yüz Yüze • Kurum"
    public CoachMeetingStatus Status { get; set; } = CoachMeetingStatus.Upcoming;
    public string? Notes { get; set; }
}
