namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public static class AcademicTrendCalculator
{
    public const decimal DefaultEpsilon = 0.03m;

    public static TrendWindowMetricsDto CalculateWindowMetrics(
        int observationCount,
        int totalQuestions,
        int correct,
        int wrong,
        int blank,
        decimal totalNet)
    {
        var attempted = correct + wrong;
        var accuracy = attempted > 0 ? Math.Round((decimal)correct / attempted, 4) : 0m;
        var successRate = totalQuestions > 0 ? Math.Round((decimal)correct / totalQuestions, 4) : 0m;
        var coverage = totalQuestions > 0 ? Math.Round((decimal)attempted / totalQuestions, 4) : 0m;
        var netPerQuestion = totalQuestions > 0 ? Math.Round(totalNet / totalQuestions, 4) : 0m;

        return new TrendWindowMetricsDto(
            observationCount,
            totalQuestions,
            correct,
            wrong,
            blank,
            totalNet,
            netPerQuestion,
            accuracy,
            successRate,
            coverage
        );
    }

    public static TrendDirection EvaluateDirection(decimal delta, decimal epsilon = DefaultEpsilon)
    {
        if (delta > epsilon) return TrendDirection.Increasing;
        if (delta < -epsilon) return TrendDirection.Decreasing;
        return TrendDirection.Stable;
    }

    public static SubjectTrendDto CalculateSubjectTrend(
        Guid subjectId,
        string subjectName,
        IReadOnlyList<SubjectObservationPointDto> points,
        int windowSize = 3,
        int? limit = 50)
    {
        var boundedPoints = points.AsEnumerable();
        if (limit.HasValue && limit.Value > 0)
        {
            boundedPoints = boundedPoints.TakeLast(limit.Value);
        }
        var pointList = boundedPoints.ToList();

        var totalObservations = pointList.Count;
        var effectiveWindowSize = Math.Clamp(windowSize, 2, 10);
        var hasEnoughData = totalObservations >= 2 * effectiveWindowSize;

        if (!hasEnoughData)
        {
            return new SubjectTrendDto(
                subjectId,
                subjectName,
                totalObservations,
                effectiveWindowSize,
                HasEnoughData: false,
                PreviousWindow: null,
                RecentWindow: null,
                SuccessRateDelta: null,
                AccuracyDelta: null,
                CoverageDelta: null,
                NetPerQuestionDelta: null,
                Direction: null,
                TimeSeries: pointList
            );
        }

        var previousSlice = pointList.Skip(totalObservations - 2 * effectiveWindowSize).Take(effectiveWindowSize).ToList();
        var recentSlice = pointList.Skip(totalObservations - effectiveWindowSize).Take(effectiveWindowSize).ToList();

        var prevMetrics = CalculateWindowMetrics(
            previousSlice.Count,
            previousSlice.Sum(p => p.TotalQuestions),
            previousSlice.Sum(p => p.Correct),
            previousSlice.Sum(p => p.Wrong),
            previousSlice.Sum(p => p.Blank),
            previousSlice.Sum(p => p.Net)
        );

        var recMetrics = CalculateWindowMetrics(
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
        var netPerQuestionDelta = Math.Round(recMetrics.NetPerQuestion - prevMetrics.NetPerQuestion, 4);
        var direction = EvaluateDirection(successRateDelta);

        return new SubjectTrendDto(
            subjectId,
            subjectName,
            totalObservations,
            effectiveWindowSize,
            HasEnoughData: true,
            PreviousWindow: prevMetrics,
            RecentWindow: recMetrics,
            SuccessRateDelta: successRateDelta,
            AccuracyDelta: accuracyDelta,
            CoverageDelta: coverageDelta,
            NetPerQuestionDelta: netPerQuestionDelta,
            Direction: direction,
            TimeSeries: pointList
        );
    }

    public static TopicTrendDto CalculateTopicTrend(
        Guid topicId,
        string topicName,
        Guid subjectId,
        string subjectName,
        IReadOnlyList<TopicObservationPointDto> points,
        int windowSize = 3,
        int? limit = 50)
    {
        var boundedPoints = points.AsEnumerable();
        if (limit.HasValue && limit.Value > 0)
        {
            boundedPoints = boundedPoints.TakeLast(limit.Value);
        }
        var pointList = boundedPoints.ToList();

        var totalObservations = pointList.Count;
        var effectiveWindowSize = Math.Clamp(windowSize, 2, 10);
        var hasEnoughData = totalObservations >= 2 * effectiveWindowSize;

        if (!hasEnoughData)
        {
            return new TopicTrendDto(
                topicId,
                topicName,
                subjectId,
                subjectName,
                totalObservations,
                effectiveWindowSize,
                HasEnoughData: false,
                PreviousWindow: null,
                RecentWindow: null,
                SuccessRateDelta: null,
                AccuracyDelta: null,
                CoverageDelta: null,
                NetPerQuestionDelta: null,
                Direction: null,
                TimeSeries: pointList
            );
        }

        var previousSlice = pointList.Skip(totalObservations - 2 * effectiveWindowSize).Take(effectiveWindowSize).ToList();
        var recentSlice = pointList.Skip(totalObservations - effectiveWindowSize).Take(effectiveWindowSize).ToList();

        var prevMetrics = CalculateWindowMetrics(
            previousSlice.Count,
            previousSlice.Sum(p => p.TotalQuestions),
            previousSlice.Sum(p => p.Correct),
            previousSlice.Sum(p => p.Wrong),
            previousSlice.Sum(p => p.Blank),
            previousSlice.Sum(p => p.Net)
        );

        var recMetrics = CalculateWindowMetrics(
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
        var netPerQuestionDelta = Math.Round(recMetrics.NetPerQuestion - prevMetrics.NetPerQuestion, 4);
        var direction = EvaluateDirection(successRateDelta);

        return new TopicTrendDto(
            topicId,
            topicName,
            subjectId,
            subjectName,
            totalObservations,
            effectiveWindowSize,
            HasEnoughData: true,
            PreviousWindow: prevMetrics,
            RecentWindow: recMetrics,
            SuccessRateDelta: successRateDelta,
            AccuracyDelta: accuracyDelta,
            CoverageDelta: coverageDelta,
            NetPerQuestionDelta: netPerQuestionDelta,
            Direction: direction,
            TimeSeries: pointList
        );
    }

    public static LearningOutcomeTrendDto CalculateLearningOutcomeTrend(
        Guid loId,
        string loCode,
        string loTitle,
        Guid topicId,
        string topicName,
        Guid subjectId,
        string subjectName,
        IReadOnlyList<LearningOutcomeObservationPointDto> points,
        int windowSize = 3,
        int? limit = 50)
    {
        var boundedPoints = points.AsEnumerable();
        if (limit.HasValue && limit.Value > 0)
        {
            boundedPoints = boundedPoints.TakeLast(limit.Value);
        }
        var pointList = boundedPoints.ToList();

        var totalObservations = pointList.Count;
        var effectiveWindowSize = Math.Clamp(windowSize, 2, 10);
        var hasEnoughData = totalObservations >= 2 * effectiveWindowSize;

        if (!hasEnoughData)
        {
            return new LearningOutcomeTrendDto(
                loId,
                loCode,
                loTitle,
                topicId,
                topicName,
                subjectId,
                subjectName,
                totalObservations,
                effectiveWindowSize,
                HasEnoughData: false,
                PreviousWindow: null,
                RecentWindow: null,
                SuccessRateDelta: null,
                AccuracyDelta: null,
                CoverageDelta: null,
                Direction: null,
                TimeSeries: pointList
            );
        }

        var previousSlice = pointList.Skip(totalObservations - 2 * effectiveWindowSize).Take(effectiveWindowSize).ToList();
        var recentSlice = pointList.Skip(totalObservations - effectiveWindowSize).Take(effectiveWindowSize).ToList();

        var prevMetrics = CalculateWindowMetrics(
            previousSlice.Count,
            previousSlice.Sum(p => p.TotalQuestions),
            previousSlice.Sum(p => p.Correct),
            previousSlice.Sum(p => p.Wrong),
            previousSlice.Sum(p => p.Blank),
            totalNet: 0m
        );

        var recMetrics = CalculateWindowMetrics(
            recentSlice.Count,
            recentSlice.Sum(p => p.TotalQuestions),
            recentSlice.Sum(p => p.Correct),
            recentSlice.Sum(p => p.Wrong),
            recentSlice.Sum(p => p.Blank),
            totalNet: 0m
        );

        var successRateDelta = Math.Round(recMetrics.SuccessRate - prevMetrics.SuccessRate, 4);
        var accuracyDelta = Math.Round(recMetrics.Accuracy - prevMetrics.Accuracy, 4);
        var coverageDelta = Math.Round(recMetrics.Coverage - prevMetrics.Coverage, 4);
        var direction = EvaluateDirection(successRateDelta);

        return new LearningOutcomeTrendDto(
            loId,
            loCode,
            loTitle,
            topicId,
            topicName,
            subjectId,
            subjectName,
            totalObservations,
            effectiveWindowSize,
            HasEnoughData: true,
            PreviousWindow: prevMetrics,
            RecentWindow: recMetrics,
            SuccessRateDelta: successRateDelta,
            AccuracyDelta: accuracyDelta,
            CoverageDelta: coverageDelta,
            Direction: direction,
            TimeSeries: pointList
        );
    }
}
