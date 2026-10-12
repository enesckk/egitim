namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public static class AcademicClassificationCalculator
{
    public static AcademicClassificationState EvaluateState(decimal successRate)
    {
        if (successRate < AcademicClassificationPolicy.CriticalThresholdUpperExclusive)
            return AcademicClassificationState.Critical;
        if (successRate < AcademicClassificationPolicy.WeakThresholdUpperExclusive)
            return AcademicClassificationState.Weak;
        if (successRate < AcademicClassificationPolicy.DevelopingThresholdUpperExclusive)
            return AcademicClassificationState.Developing;
        return AcademicClassificationState.Strong;
    }

    public static SubjectClassificationDto CalculateSubjectClassification(SubjectTrendDto trendDto)
    {
        var points = trendDto.TimeSeries;
        var overallObsCount = points.Count;
        var overallTotalQ = points.Sum(p => p.TotalQuestions);
        var overallCorrect = points.Sum(p => p.Correct);
        var overallWrong = points.Sum(p => p.Wrong);
        var overallBlank = points.Sum(p => p.Blank);
        var overallAttempted = overallCorrect + overallWrong;

        var overallSuccessRate = overallTotalQ > 0 ? Math.Round((decimal)overallCorrect / overallTotalQ, 4) : 0m;
        var overallAccuracy = overallAttempted > 0 ? Math.Round((decimal)overallCorrect / overallAttempted, 4) : 0m;
        var overallCoverage = overallTotalQ > 0 ? Math.Round((decimal)overallAttempted / overallTotalQ, 4) : 0m;

        bool isOverallSufficient = overallObsCount >= AcademicClassificationPolicy.SubjectMinObservations &&
                                   overallTotalQ >= AcademicClassificationPolicy.SubjectMinQuestions;

        bool isRecentSufficient = trendDto.HasEnoughData &&
                                  trendDto.RecentWindow != null &&
                                  trendDto.RecentWindow.ObservationCount >= AcademicClassificationPolicy.SubjectMinObservations &&
                                  trendDto.RecentWindow.TotalQuestions >= AcademicClassificationPolicy.SubjectMinQuestions;

        bool hasEnoughData = isOverallSufficient || isRecentSufficient;

        ClassificationInsufficiencyReason reason;
        if (overallObsCount == 0)
        {
            reason = ClassificationInsufficiencyReason.NoData;
        }
        else if (!hasEnoughData)
        {
            reason = overallObsCount < AcademicClassificationPolicy.SubjectMinObservations
                ? ClassificationInsufficiencyReason.TooFewObservations
                : ClassificationInsufficiencyReason.TooFewQuestions;
        }
        else
        {
            reason = ClassificationInsufficiencyReason.None;
        }

        AcademicClassificationState state;
        ClassificationBasis basis;
        int obsCount;
        int totalQ;
        int attempted;
        int correct;
        int wrong;
        int blank;
        decimal effectiveSuccessRate;
        decimal effectiveAccuracy;
        decimal effectiveCoverage;

        if (hasEnoughData)
        {
            if (isRecentSufficient)
            {
                basis = ClassificationBasis.RecentWindow;
                var rw = trendDto.RecentWindow!;
                obsCount = rw.ObservationCount;
                totalQ = rw.TotalQuestions;
                correct = rw.Correct;
                wrong = rw.Wrong;
                blank = rw.Blank;
                attempted = correct + wrong;
                effectiveSuccessRate = rw.SuccessRate;
                effectiveAccuracy = rw.Accuracy;
                effectiveCoverage = rw.Coverage;
            }
            else
            {
                basis = ClassificationBasis.OverallHistory;
                obsCount = overallObsCount;
                totalQ = overallTotalQ;
                correct = overallCorrect;
                wrong = overallWrong;
                blank = overallBlank;
                attempted = overallAttempted;
                effectiveSuccessRate = overallSuccessRate;
                effectiveAccuracy = overallAccuracy;
                effectiveCoverage = overallCoverage;
            }

            state = EvaluateState(effectiveSuccessRate);
        }
        else
        {
            state = AcademicClassificationState.InsufficientData;
            basis = ClassificationBasis.OverallHistory;
            obsCount = overallObsCount;
            totalQ = overallTotalQ;
            correct = overallCorrect;
            wrong = overallWrong;
            blank = overallBlank;
            attempted = overallAttempted;
            effectiveSuccessRate = overallSuccessRate;
            effectiveAccuracy = overallAccuracy;
            effectiveCoverage = overallCoverage;
        }

        return new SubjectClassificationDto(
            trendDto.SubjectId,
            trendDto.SubjectName,
            state,
            hasEnoughData,
            basis,
            obsCount,
            totalQ,
            attempted,
            correct,
            wrong,
            blank,
            effectiveSuccessRate,
            effectiveAccuracy,
            effectiveCoverage,
            trendDto.Direction,
            trendDto.SuccessRateDelta,
            AcademicClassificationPolicy.CurrentPolicyVersion,
            reason
        );
    }

    public static TopicClassificationDto CalculateTopicClassification(TopicTrendDto trendDto)
    {
        var points = trendDto.TimeSeries;
        var overallObsCount = points.Count;
        var overallTotalQ = points.Sum(p => p.TotalQuestions);
        var overallCorrect = points.Sum(p => p.Correct);
        var overallWrong = points.Sum(p => p.Wrong);
        var overallBlank = points.Sum(p => p.Blank);
        var overallAttempted = overallCorrect + overallWrong;

        var overallSuccessRate = overallTotalQ > 0 ? Math.Round((decimal)overallCorrect / overallTotalQ, 4) : 0m;
        var overallAccuracy = overallAttempted > 0 ? Math.Round((decimal)overallCorrect / overallAttempted, 4) : 0m;
        var overallCoverage = overallTotalQ > 0 ? Math.Round((decimal)overallAttempted / overallTotalQ, 4) : 0m;

        bool isOverallSufficient = overallObsCount >= AcademicClassificationPolicy.TopicMinObservations &&
                                   overallTotalQ >= AcademicClassificationPolicy.TopicMinQuestions;

        bool isRecentSufficient = trendDto.HasEnoughData &&
                                  trendDto.RecentWindow != null &&
                                  trendDto.RecentWindow.ObservationCount >= AcademicClassificationPolicy.TopicMinObservations &&
                                  trendDto.RecentWindow.TotalQuestions >= AcademicClassificationPolicy.TopicMinQuestions;

        bool hasEnoughData = isOverallSufficient || isRecentSufficient;

        ClassificationInsufficiencyReason reason;
        if (overallObsCount == 0)
        {
            reason = ClassificationInsufficiencyReason.NoData;
        }
        else if (!hasEnoughData)
        {
            reason = overallObsCount < AcademicClassificationPolicy.TopicMinObservations
                ? ClassificationInsufficiencyReason.TooFewObservations
                : ClassificationInsufficiencyReason.TooFewQuestions;
        }
        else
        {
            reason = ClassificationInsufficiencyReason.None;
        }

        AcademicClassificationState state;
        ClassificationBasis basis;
        int obsCount;
        int totalQ;
        int attempted;
        int correct;
        int wrong;
        int blank;
        decimal effectiveSuccessRate;
        decimal effectiveAccuracy;
        decimal effectiveCoverage;

        if (hasEnoughData)
        {
            if (isRecentSufficient)
            {
                basis = ClassificationBasis.RecentWindow;
                var rw = trendDto.RecentWindow!;
                obsCount = rw.ObservationCount;
                totalQ = rw.TotalQuestions;
                correct = rw.Correct;
                wrong = rw.Wrong;
                blank = rw.Blank;
                attempted = correct + wrong;
                effectiveSuccessRate = rw.SuccessRate;
                effectiveAccuracy = rw.Accuracy;
                effectiveCoverage = rw.Coverage;
            }
            else
            {
                basis = ClassificationBasis.OverallHistory;
                obsCount = overallObsCount;
                totalQ = overallTotalQ;
                correct = overallCorrect;
                wrong = overallWrong;
                blank = overallBlank;
                attempted = overallAttempted;
                effectiveSuccessRate = overallSuccessRate;
                effectiveAccuracy = overallAccuracy;
                effectiveCoverage = overallCoverage;
            }

            state = EvaluateState(effectiveSuccessRate);
        }
        else
        {
            state = AcademicClassificationState.InsufficientData;
            basis = ClassificationBasis.OverallHistory;
            obsCount = overallObsCount;
            totalQ = overallTotalQ;
            correct = overallCorrect;
            wrong = overallWrong;
            blank = overallBlank;
            attempted = overallAttempted;
            effectiveSuccessRate = overallSuccessRate;
            effectiveAccuracy = overallAccuracy;
            effectiveCoverage = overallCoverage;
        }

        return new TopicClassificationDto(
            trendDto.TopicId,
            trendDto.TopicName,
            trendDto.SubjectId,
            trendDto.SubjectName,
            state,
            hasEnoughData,
            basis,
            obsCount,
            totalQ,
            attempted,
            correct,
            wrong,
            blank,
            effectiveSuccessRate,
            effectiveAccuracy,
            effectiveCoverage,
            trendDto.Direction,
            trendDto.SuccessRateDelta,
            AcademicClassificationPolicy.CurrentPolicyVersion,
            reason
        );
    }

    public static LearningOutcomeClassificationDto CalculateLearningOutcomeClassification(LearningOutcomeTrendDto trendDto)
    {
        var points = trendDto.TimeSeries;
        var overallObsCount = points.Count;
        var overallTotalQ = points.Sum(p => p.TotalQuestions);
        var overallCorrect = points.Sum(p => p.Correct);
        var overallWrong = points.Sum(p => p.Wrong);
        var overallBlank = points.Sum(p => p.Blank);
        var overallAttempted = overallCorrect + overallWrong;

        var overallSuccessRate = overallTotalQ > 0 ? Math.Round((decimal)overallCorrect / overallTotalQ, 4) : 0m;
        var overallAccuracy = overallAttempted > 0 ? Math.Round((decimal)overallCorrect / overallAttempted, 4) : 0m;
        var overallCoverage = overallTotalQ > 0 ? Math.Round((decimal)overallAttempted / overallTotalQ, 4) : 0m;

        bool isOverallSufficient = overallObsCount >= AcademicClassificationPolicy.LearningOutcomeMinObservations &&
                                   overallTotalQ >= AcademicClassificationPolicy.LearningOutcomeMinQuestions;

        bool isRecentSufficient = trendDto.HasEnoughData &&
                                  trendDto.RecentWindow != null &&
                                  trendDto.RecentWindow.ObservationCount >= AcademicClassificationPolicy.LearningOutcomeMinObservations &&
                                  trendDto.RecentWindow.TotalQuestions >= AcademicClassificationPolicy.LearningOutcomeMinQuestions;

        bool hasEnoughData = isOverallSufficient || isRecentSufficient;

        ClassificationInsufficiencyReason reason;
        if (overallObsCount == 0)
        {
            reason = ClassificationInsufficiencyReason.NoData;
        }
        else if (!hasEnoughData)
        {
            reason = overallObsCount < AcademicClassificationPolicy.LearningOutcomeMinObservations
                ? ClassificationInsufficiencyReason.TooFewObservations
                : ClassificationInsufficiencyReason.TooFewQuestions;
        }
        else
        {
            reason = ClassificationInsufficiencyReason.None;
        }

        AcademicClassificationState state;
        ClassificationBasis basis;
        int obsCount;
        int totalQ;
        int attempted;
        int correct;
        int wrong;
        int blank;
        decimal effectiveSuccessRate;
        decimal effectiveAccuracy;
        decimal effectiveCoverage;

        if (hasEnoughData)
        {
            if (isRecentSufficient)
            {
                basis = ClassificationBasis.RecentWindow;
                var rw = trendDto.RecentWindow!;
                obsCount = rw.ObservationCount;
                totalQ = rw.TotalQuestions;
                correct = rw.Correct;
                wrong = rw.Wrong;
                blank = rw.Blank;
                attempted = correct + wrong;
                effectiveSuccessRate = rw.SuccessRate;
                effectiveAccuracy = rw.Accuracy;
                effectiveCoverage = rw.Coverage;
            }
            else
            {
                basis = ClassificationBasis.OverallHistory;
                obsCount = overallObsCount;
                totalQ = overallTotalQ;
                correct = overallCorrect;
                wrong = overallWrong;
                blank = overallBlank;
                attempted = overallAttempted;
                effectiveSuccessRate = overallSuccessRate;
                effectiveAccuracy = overallAccuracy;
                effectiveCoverage = overallCoverage;
            }

            state = EvaluateState(effectiveSuccessRate);
        }
        else
        {
            state = AcademicClassificationState.InsufficientData;
            basis = ClassificationBasis.OverallHistory;
            obsCount = overallObsCount;
            totalQ = overallTotalQ;
            correct = overallCorrect;
            wrong = overallWrong;
            blank = overallBlank;
            attempted = overallAttempted;
            effectiveSuccessRate = overallSuccessRate;
            effectiveAccuracy = overallAccuracy;
            effectiveCoverage = overallCoverage;
        }

        return new LearningOutcomeClassificationDto(
            trendDto.LearningOutcomeId,
            trendDto.LearningOutcomeCode,
            trendDto.LearningOutcomeTitle,
            trendDto.TopicId,
            trendDto.TopicName,
            trendDto.SubjectId,
            trendDto.SubjectName,
            state,
            hasEnoughData,
            basis,
            obsCount,
            totalQ,
            attempted,
            correct,
            wrong,
            blank,
            effectiveSuccessRate,
            effectiveAccuracy,
            effectiveCoverage,
            trendDto.Direction,
            trendDto.SuccessRateDelta,
            AcademicClassificationPolicy.CurrentPolicyVersion,
            reason
        );
    }
}
