using EgitimPlatform.Modules.Coaching.Entities;

namespace EgitimPlatform.Modules.Coaching.Features.ManageMeetings;

public sealed record CoachMeetingDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string Initials,
    string Grade,
    string Field,
    string DateStr,
    string TimeStr,
    int DurationMinutes,
    string Type,
    string LocationType,
    string Status, // 'upcoming' | 'completed' | 'cancelled'
    string? Notes,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime
);

public sealed record CreateCoachMeetingRequest(
    Guid StudentId,
    DateTimeOffset StartTime,
    int DurationMinutes,
    string MeetingType,
    string LocationType,
    string? Notes
);

public sealed record UpdateCoachMeetingRequest(
    DateTimeOffset? StartTime,
    int? DurationMinutes,
    string? MeetingType,
    string? LocationType,
    string? Status,
    string? Notes
);
