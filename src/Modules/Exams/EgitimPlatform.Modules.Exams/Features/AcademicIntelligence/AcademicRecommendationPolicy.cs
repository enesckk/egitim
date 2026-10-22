namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public static class AcademicRecommendationPolicy
{
    public const string CurrentRecommendationPolicyVersion = "v1";

    // Recommendation Result Bounds
    public const int DefaultRecommendationLimit = 10;
    public const int MinRecommendationLimit = 1;
    public const int MaxRecommendationLimit = 25;

    // Base Priority Scores by State (strictly non-overlapping ranges: Critical 71-100, Weak 36-68, Developing 1-33)
    public const int BaseScoreCritical = 70;
    public const int BaseScoreWeak = 35;
    public const int BaseScoreDeveloping = 0;
    public const int BaseScoreStrong = 0;
    public const int BaseScoreInsufficientData = 0;

    // Trend Urgency Modifiers
    public const int TrendModifierDecreasing = 15;
    public const int TrendModifierStable = 5;
    public const int TrendModifierIncreasing = 0;

    // Specificity Level Bonuses (Tie-breaker)
    public const int SpecificityBonusLearningOutcome = 3;
    public const int SpecificityBonusTopic = 2;
    public const int SpecificityBonusQuestionStyle = 2;
    public const int SpecificityBonusSubject = 1;

    // Maximum Bonus for Low Success Rate
    public const int MaxLowSuccessRateBonus = 15;
}
