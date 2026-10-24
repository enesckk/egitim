namespace EgitimPlatform.Modules.Communication.Features.ManagePreferences;

public record NotificationPreferencesDto(
    bool InAppEnabled,
    bool ExamResultsEnabled,
    bool CoachTasksEnabled,
    bool MeetingsEnabled,
    bool MessagesEnabled,
    bool MaterialsEnabled
);

public record UpdateNotificationPreferencesRequest(
    bool InAppEnabled,
    bool ExamResultsEnabled,
    bool CoachTasksEnabled,
    bool MeetingsEnabled,
    bool MessagesEnabled,
    bool MaterialsEnabled
);
