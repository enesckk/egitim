namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public static class AcademicRecommendationCalculator
{
    public static int CalculatePriorityScore(
        AcademicClassificationState state,
        TrendDirection? trend,
        decimal? successRate,
        RecommendationNodeType nodeType)
    {
        if (state == AcademicClassificationState.InsufficientData)
            return 0;

        int baseScore = state switch
        {
            AcademicClassificationState.Critical => AcademicRecommendationPolicy.BaseScoreCritical,
            AcademicClassificationState.Weak => AcademicRecommendationPolicy.BaseScoreWeak,
            AcademicClassificationState.Developing => AcademicRecommendationPolicy.BaseScoreDeveloping,
            AcademicClassificationState.Strong => AcademicRecommendationPolicy.BaseScoreStrong,
            _ => 0
        };

        int trendModifier = trend switch
        {
            TrendDirection.Decreasing => AcademicRecommendationPolicy.TrendModifierDecreasing,
            TrendDirection.Stable => AcademicRecommendationPolicy.TrendModifierStable,
            TrendDirection.Increasing => AcademicRecommendationPolicy.TrendModifierIncreasing,
            _ => 0
        };

        int lowSuccessBonus = 0;
        if (successRate.HasValue)
        {
            var clampedSr = Math.Clamp(successRate.Value, 0m, 1m);
            lowSuccessBonus = (int)Math.Round((1.0m - clampedSr) * AcademicRecommendationPolicy.MaxLowSuccessRateBonus);
        }

        int specificityBonus = nodeType switch
        {
            RecommendationNodeType.LearningOutcome => AcademicRecommendationPolicy.SpecificityBonusLearningOutcome,
            RecommendationNodeType.Topic => AcademicRecommendationPolicy.SpecificityBonusTopic,
            RecommendationNodeType.QuestionStyle => AcademicRecommendationPolicy.SpecificityBonusQuestionStyle,
            RecommendationNodeType.Subject => AcademicRecommendationPolicy.SpecificityBonusSubject,
            _ => 0
        };

        int rawScore = baseScore + trendModifier + lowSuccessBonus + specificityBonus;
        return Math.Clamp(rawScore, 0, 100);
    }

    public static List<AcademicRecommendationDto> SortAndLimitRecommendations(
        IEnumerable<AcademicRecommendationDto> candidates,
        int limit)
    {
        var clampedLimit = Math.Clamp(
            limit,
            AcademicRecommendationPolicy.MinRecommendationLimit,
            AcademicRecommendationPolicy.MaxRecommendationLimit
        );

        return candidates
            .OrderByDescending(c => GetClassificationSeverityPriority(c.ClassificationState))
            .ThenByDescending(c => c.PriorityScore)
            .ThenByDescending(c => GetNodeTypePriority(c.NodeType))
            .ThenBy(c => c.SuccessRate ?? 2.0m) // Null rates last
            .ThenBy(c => c.Code)
            .ThenBy(c => c.Name)
            .ThenBy(c => c.NodeId)
            .Take(clampedLimit)
            .ToList();
    }

    private static int GetClassificationSeverityPriority(AcademicClassificationState state) => state switch
    {
        AcademicClassificationState.Critical => 3,
        AcademicClassificationState.Weak => 2,
        AcademicClassificationState.Developing => 1,
        _ => 0
    };

    private static int GetNodeTypePriority(RecommendationNodeType nodeType) => nodeType switch
    {
        RecommendationNodeType.LearningOutcome => 4,
        RecommendationNodeType.Topic => 3,
        RecommendationNodeType.QuestionStyle => 2,
        RecommendationNodeType.Subject => 1,
        _ => 0
    };
}
