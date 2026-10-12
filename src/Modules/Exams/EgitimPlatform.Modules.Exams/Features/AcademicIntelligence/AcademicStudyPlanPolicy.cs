namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public static class AcademicStudyPlanPolicy
{
    public const string CurrentStudyPlanPolicyVersion = "v1";

    // Plan Horizon Bounds (Days)
    public const int DefaultPlanDays = 7;
    public const int MinPlanDays = 1;
    public const int MaxPlanDays = 14;

    // Daily Academic Study Capacity Bounds (Minutes)
    public const int DefaultDailyStudyMinutes = 120;
    public const int MinDailyStudyMinutes = 30;
    public const int MaxDailyStudyMinutes = 480;

    // Bounded Study Session Duration (Minutes)
    public const int SessionMinutes = 30;

    // Maximum Capacity Share Guard per Single Candidate (40% of total plan sessions)
    public const decimal MaxShareRatio = 0.40m;
}
