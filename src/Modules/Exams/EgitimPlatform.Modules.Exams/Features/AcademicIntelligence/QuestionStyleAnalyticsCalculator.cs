namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public record QuestionStyleMeta(Guid Id, string Code, string Name, int DisplayOrder);

public record RawQuestionAttemptObservation(
    Guid AttemptId,
    Guid ExamId,
    string ExamTitle,
    DateTimeOffset TakenAt,
    int AttemptNumber,
    Guid QuestionId,
    IReadOnlyCollection<Guid> AssignedStyleIds,
    bool? IsCorrect,
    bool IsBlank,
    decimal NetContribution
);

public static class QuestionStyleAnalyticsCalculator
{
    private static int GetClassificationSortOrder(AcademicClassificationState state) => state switch
    {
        AcademicClassificationState.Critical => 1,
        AcademicClassificationState.Weak => 2,
        AcademicClassificationState.Developing => 3,
        AcademicClassificationState.Strong => 4,
        AcademicClassificationState.InsufficientData => 5,
        _ => 6
    };

    public static List<StudentQuestionStyleAnalyticsDto> CalculateAnalytics(
        IReadOnlyList<QuestionStyleMeta> activeStyles,
        IReadOnlyList<RawQuestionAttemptObservation> observations,
        int windowSize = 2)
    {
        var result = new List<StudentQuestionStyleAnalyticsDto>();

        foreach (var style in activeStyles)
        {
            var styleObs = observations
                .Where(o => o.AssignedStyleIds.Contains(style.Id))
                .ToList();

            var distinctAttempts = styleObs.Select(o => o.AttemptId).Distinct().Count();
            var totalQuestions = styleObs.Count;
            var correctCount = styleObs.Count(o => o.IsCorrect == true && !o.IsBlank);
            var incorrectCount = styleObs.Count(o => o.IsCorrect == false && !o.IsBlank);
            var blankCount = styleObs.Count(o => o.IsBlank);
            var attemptedQuestions = correctCount + incorrectCount;

            decimal? accuracy = null;
            decimal? successRate = null;
            decimal coverage = 0m;
            AcademicClassificationState classificationState;

            if (totalQuestions == 0)
            {
                classificationState = AcademicClassificationState.InsufficientData;
            }
            else
            {
                if (attemptedQuestions > 0)
                {
                    accuracy = Math.Round((decimal)correctCount / attemptedQuestions, 4);
                }

                successRate = Math.Round((decimal)correctCount / totalQuestions, 4);
                coverage = Math.Round((decimal)attemptedQuestions / totalQuestions, 4);

                bool isSufficient = distinctAttempts >= AcademicClassificationPolicy.QuestionStyleMinObservations &&
                                   totalQuestions >= AcademicClassificationPolicy.QuestionStyleMinQuestions;

                classificationState = isSufficient
                    ? AcademicClassificationCalculator.EvaluateState(successRate.Value)
                    : AcademicClassificationState.InsufficientData;
            }

            // Calculate trend direction if enough chronological observations exist
            TrendDirection? trendDirection = null;
            var points = BuildTimeSeries(style.Id, styleObs);
            var effectiveWindowSize = Math.Clamp(windowSize, 2, 10);
            if (points.Count >= 2 * effectiveWindowSize)
            {
                var prevSlice = points.Skip(points.Count - 2 * effectiveWindowSize).Take(effectiveWindowSize).ToList();
                var recSlice = points.Skip(points.Count - effectiveWindowSize).Take(effectiveWindowSize).ToList();

                var prevSR = prevSlice.Sum(p => p.TotalQuestions) > 0
                    ? Math.Round((decimal)prevSlice.Sum(p => p.Correct) / prevSlice.Sum(p => p.TotalQuestions), 4)
                    : 0m;

                var recSR = recSlice.Sum(p => p.TotalQuestions) > 0
                    ? Math.Round((decimal)recSlice.Sum(p => p.Correct) / recSlice.Sum(p => p.TotalQuestions), 4)
                    : 0m;

                var delta = Math.Round(recSR - prevSR, 4);
                trendDirection = AcademicTrendCalculator.EvaluateDirection(delta);
            }

            result.Add(new StudentQuestionStyleAnalyticsDto(
                style.Id,
                style.Code,
                style.Name,
                totalQuestions,
                attemptedQuestions,
                correctCount,
                incorrectCount,
                blankCount,
                accuracy,
                successRate,
                coverage,
                distinctAttempts,
                classificationState,
                trendDirection
            ));
        }

        // Deterministic sorting:
        // 1. Critical, Weak, Developing, Strong, InsufficientData
        // 2. Lowest SuccessRate first (nulls sorted deterministically after known values)
        // 3. DisplayOrder
        // 4. Name
        return result
            .OrderBy(x => GetClassificationSortOrder(x.ClassificationState))
            .ThenBy(x => x.SuccessRate ?? decimal.MaxValue)
            .ThenBy(x => activeStyles.FirstOrDefault(s => s.Id == x.QuestionStyleId)?.DisplayOrder ?? 999)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public static List<QuestionStyleTrendDto> CalculateTrends(
        IReadOnlyList<QuestionStyleMeta> activeStyles,
        IReadOnlyList<RawQuestionAttemptObservation> observations,
        int windowSize = 2,
        int? limit = 50)
    {
        var result = new List<QuestionStyleTrendDto>();
        var effectiveWindowSize = Math.Clamp(windowSize, 2, 10);

        foreach (var style in activeStyles)
        {
            var styleObs = observations
                .Where(o => o.AssignedStyleIds.Contains(style.Id))
                .ToList();

            var points = BuildTimeSeries(style.Id, styleObs);
            if (limit.HasValue && limit.Value > 0 && points.Count > limit.Value)
            {
                points = points.TakeLast(limit.Value).ToList();
            }

            var totalObservations = points.Count;
            var hasEnoughData = totalObservations >= 2 * effectiveWindowSize;

            if (!hasEnoughData)
            {
                result.Add(new QuestionStyleTrendDto(
                    style.Id,
                    style.Code,
                    style.Name,
                    totalObservations,
                    effectiveWindowSize,
                    HasEnoughData: false,
                    PreviousWindow: null,
                    RecentWindow: null,
                    SuccessRateDelta: null,
                    AccuracyDelta: null,
                    CoverageDelta: null,
                    Direction: null,
                    TimeSeries: points
                ));
                continue;
            }

            var previousSlice = points.Skip(totalObservations - 2 * effectiveWindowSize).Take(effectiveWindowSize).ToList();
            var recentSlice = points.Skip(totalObservations - effectiveWindowSize).Take(effectiveWindowSize).ToList();

            var prevMetrics = AcademicTrendCalculator.CalculateWindowMetrics(
                previousSlice.Count,
                previousSlice.Sum(p => p.TotalQuestions),
                previousSlice.Sum(p => p.Correct),
                previousSlice.Sum(p => p.Wrong),
                previousSlice.Sum(p => p.Blank),
                previousSlice.Sum(p => p.Net)
            );

            var recMetrics = AcademicTrendCalculator.CalculateWindowMetrics(
                recentSlice.Count,
                recentSlice.Sum(p => p.TotalQuestions),
                recentSlice.Sum(p => p.Correct),
                recentSlice.Sum(p => p.Wrong),
                recentSlice.Sum(p => p.Blank),
                recentSlice.Sum(p => p.Net)
            );

            var successRateDelta = Math.Round(recMetrics.SuccessRate - prevMetrics.SuccessRate, 4);
            var accuracyDelta = Math.Round(recMetrics.Accuracy - prevMetrics.Accuracy, 4);
            var coverageDelta = Math.Round(recMetrics.Coverage - prevMetrics.Coverage, 4);
            var direction = AcademicTrendCalculator.EvaluateDirection(successRateDelta);

            result.Add(new QuestionStyleTrendDto(
                style.Id,
                style.Code,
                style.Name,
                totalObservations,
                effectiveWindowSize,
                HasEnoughData: true,
                PreviousWindow: prevMetrics,
                RecentWindow: recMetrics,
                SuccessRateDelta: successRateDelta,
                AccuracyDelta: accuracyDelta,
                CoverageDelta: coverageDelta,
                Direction: direction,
                TimeSeries: points
            ));
        }

        return result
            .OrderBy(x => activeStyles.FirstOrDefault(s => s.Id == x.QuestionStyleId)?.DisplayOrder ?? 999)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static List<QuestionStyleObservationPointDto> BuildTimeSeries(
        Guid styleId,
        IReadOnlyList<RawQuestionAttemptObservation> observations)
    {
        var groupedByAttempt = observations
            .GroupBy(o => new { o.AttemptId, o.ExamId, o.ExamTitle, o.TakenAt, o.AttemptNumber })
            .OrderBy(g => g.Key.TakenAt)
            .ThenBy(g => g.Key.AttemptNumber)
            .ToList();

        var points = new List<QuestionStyleObservationPointDto>();

        foreach (var g in groupedByAttempt)
        {
            var totalQ = g.Count();
            var correct = g.Count(o => o.IsCorrect == true && !o.IsBlank);
            var wrong = g.Count(o => o.IsCorrect == false && !o.IsBlank);
            var blank = g.Count(o => o.IsBlank);
            var attempted = correct + wrong;
            var net = g.Sum(o => o.NetContribution);

            decimal? accuracy = attempted > 0 ? Math.Round((decimal)correct / attempted, 4) : null;
            decimal? successRate = totalQ > 0 ? Math.Round((decimal)correct / totalQ, 4) : null;
            decimal coverage = totalQ > 0 ? Math.Round((decimal)attempted / totalQ, 4) : 0m;

            points.Add(new QuestionStyleObservationPointDto(
                g.Key.AttemptId,
                g.Key.ExamId,
                g.Key.ExamTitle,
                g.Key.TakenAt,
                g.Key.AttemptNumber,
                styleId,
                totalQ,
                correct,
                wrong,
                blank,
                net,
                accuracy,
                successRate,
                coverage
            ));
        }

        return points;
    }
}
