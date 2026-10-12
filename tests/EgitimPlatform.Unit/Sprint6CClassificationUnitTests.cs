using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint6CClassificationUnitTests
{
    [Theory]
    [InlineData(0.0000, AcademicClassificationState.Critical)]
    [InlineData(0.3999, AcademicClassificationState.Critical)]
    [InlineData(0.4000, AcademicClassificationState.Weak)]
    [InlineData(0.5999, AcademicClassificationState.Weak)]
    [InlineData(0.6000, AcademicClassificationState.Developing)]
    [InlineData(0.7999, AcademicClassificationState.Developing)]
    [InlineData(0.8000, AcademicClassificationState.Strong)]
    [InlineData(1.0000, AcademicClassificationState.Strong)]
    public void EvaluateState_Boundaries_CorrectlyClassifiesAllRanges(decimal successRate, AcademicClassificationState expectedState)
    {
        var actualState = AcademicClassificationCalculator.EvaluateState(successRate);
        Assert.Equal(expectedState, actualState);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public void CalculateSubjectClassification_InsufficientData_TooFewObservations()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var points = new List<SubjectObservationPointDto>
        {
            CreateSubjectPoint(subjectId, now, total: 15, correct: 10) // 1 obs, 15 questions
        };

        var trendDto = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Matematik", points, windowSize: 3);
        var classification = AcademicClassificationCalculator.CalculateSubjectClassification(trendDto);

        Assert.False(classification.HasEnoughData);
        Assert.Equal(AcademicClassificationState.InsufficientData, classification.State);
        Assert.Equal(ClassificationInsufficiencyReason.TooFewObservations, classification.InsufficiencyReason);
        Assert.Equal(ClassificationBasis.OverallHistory, classification.Basis);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public void CalculateSubjectClassification_InsufficientData_TooFewQuestions()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var points = new List<SubjectObservationPointDto>
        {
            CreateSubjectPoint(subjectId, now.AddDays(-2), total: 4, correct: 2),
            CreateSubjectPoint(subjectId, now.AddDays(-1), total: 4, correct: 2) // 2 obs, 8 questions total (< 10)
        };

        var trendDto = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Fizik", points, windowSize: 3);
        var classification = AcademicClassificationCalculator.CalculateSubjectClassification(trendDto);

        Assert.False(classification.HasEnoughData);
        Assert.Equal(AcademicClassificationState.InsufficientData, classification.State);
        Assert.Equal(ClassificationInsufficiencyReason.TooFewQuestions, classification.InsufficiencyReason);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public void CalculateSubjectClassification_NoData_ReturnsNoDataReason()
    {
        var subjectId = Guid.NewGuid();
        var trendDto = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Kimya", [], windowSize: 3);

        var classification = AcademicClassificationCalculator.CalculateSubjectClassification(trendDto);

        Assert.False(classification.HasEnoughData);
        Assert.Equal(AcademicClassificationState.InsufficientData, classification.State);
        Assert.Equal(ClassificationInsufficiencyReason.NoData, classification.InsufficiencyReason);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public void CalculateSubjectClassification_RecentWindowPrecedence_WhenTrendHasEnoughData()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 6 points (windowSize = 3)
        // Previous Window (1..3): 80% success rate
        // Recent Window (4..6): 30% success rate
        var points = new List<SubjectObservationPointDto>
        {
            CreateSubjectPoint(subjectId, now.AddDays(-6), total: 10, correct: 8),
            CreateSubjectPoint(subjectId, now.AddDays(-5), total: 10, correct: 8),
            CreateSubjectPoint(subjectId, now.AddDays(-4), total: 10, correct: 8),
            CreateSubjectPoint(subjectId, now.AddDays(-3), total: 10, correct: 3),
            CreateSubjectPoint(subjectId, now.AddDays(-2), total: 10, correct: 3),
            CreateSubjectPoint(subjectId, now.AddDays(-1), total: 10, correct: 3)
        };

        var trendDto = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Biyoloji", points, windowSize: 3);
        var classification = AcademicClassificationCalculator.CalculateSubjectClassification(trendDto);

        Assert.True(classification.HasEnoughData);
        Assert.Equal(ClassificationBasis.RecentWindow, classification.Basis);
        Assert.Equal(0.3000m, classification.SuccessRate);
        Assert.Equal(AcademicClassificationState.Critical, classification.State);
        Assert.Equal(TrendDirection.Decreasing, classification.TrendDirection);
        Assert.Equal(-0.5000m, classification.SuccessRateDelta);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public void CalculateSubjectClassification_RecentWindowInsufficientQuestions_FallsBackToOverallHistory()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 6 observations total (windowSize = 3)
        // Previous Window (points 1..3): 10 questions each, 7 correct (70% success)
        // Recent Window (points 4..6): 2 questions each, 2 correct (100% success rate, but only 6 total questions < SubjectMinQuestions 10)
        var points = new List<SubjectObservationPointDto>
        {
            CreateSubjectPoint(subjectId, now.AddDays(-6), total: 10, correct: 7),
            CreateSubjectPoint(subjectId, now.AddDays(-5), total: 10, correct: 7),
            CreateSubjectPoint(subjectId, now.AddDays(-4), total: 10, correct: 7),
            CreateSubjectPoint(subjectId, now.AddDays(-3), total: 2, correct: 2),
            CreateSubjectPoint(subjectId, now.AddDays(-2), total: 2, correct: 2),
            CreateSubjectPoint(subjectId, now.AddDays(-1), total: 2, correct: 2)
        };

        var trendDto = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Geometri", points, windowSize: 3);
        var classification = AcademicClassificationCalculator.CalculateSubjectClassification(trendDto);

        // Overall: 36/36 questions = 75% -> Developing
        // Recent window has 6 questions (< 10 SubjectMinQuestions) -> Must fall back to OverallHistory!
        Assert.True(classification.HasEnoughData);
        Assert.Equal(ClassificationBasis.OverallHistory, classification.Basis);
        Assert.Equal(36, classification.TotalQuestions);
        Assert.Equal(0.7500m, classification.SuccessRate);
        Assert.Equal(AcademicClassificationState.Developing, classification.State);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public void CalculateSubjectClassification_MetricSnapshotMatchesSelectedBasis()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 6 observations, each with 10 questions.
        // Previous window (1..3): 40% success (12/30)
        // Recent window (4..6): 90% success (27/30)
        var points = new List<SubjectObservationPointDto>
        {
            CreateSubjectPoint(subjectId, now.AddDays(-6), total: 10, correct: 4),
            CreateSubjectPoint(subjectId, now.AddDays(-5), total: 10, correct: 4),
            CreateSubjectPoint(subjectId, now.AddDays(-4), total: 10, correct: 4),
            CreateSubjectPoint(subjectId, now.AddDays(-3), total: 10, correct: 9),
            CreateSubjectPoint(subjectId, now.AddDays(-2), total: 10, correct: 9),
            CreateSubjectPoint(subjectId, now.AddDays(-1), total: 10, correct: 9)
        };

        var trendDto = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Fizik", points, windowSize: 3);
        var classification = AcademicClassificationCalculator.CalculateSubjectClassification(trendDto);

        Assert.True(classification.HasEnoughData);
        Assert.Equal(ClassificationBasis.RecentWindow, classification.Basis);

        // ALL metrics must strictly match RecentWindow (3 observations, 30 questions, 27 correct, 3 wrong, 0 blank)
        Assert.Equal(3, classification.ObservationCount);
        Assert.Equal(30, classification.TotalQuestions);
        Assert.Equal(27, classification.TotalCorrect);
        Assert.Equal(3, classification.TotalWrong);
        Assert.Equal(0, classification.TotalBlank);
        Assert.Equal(30, classification.AttemptedCount);
        Assert.Equal(0.9000m, classification.SuccessRate);
        Assert.Equal(0.9000m, classification.Accuracy);
        Assert.Equal(1.0000m, classification.Coverage);
        Assert.Equal(AcademicClassificationState.Strong, classification.State);
    }


    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public void CalculateSubjectClassification_AllBlank_ReturnsCriticalWhenSufficientSample()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 2 obs, 20 questions total, all blank -> correct = 0, wrong = 0, blank = 20
        var points = new List<SubjectObservationPointDto>
        {
            CreateSubjectPoint(subjectId, now.AddDays(-2), total: 10, correct: 0, wrong: 0, blank: 10),
            CreateSubjectPoint(subjectId, now.AddDays(-1), total: 10, correct: 0, wrong: 0, blank: 10)
        };

        var trendDto = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Coğrafya", points, windowSize: 3);
        var classification = AcademicClassificationCalculator.CalculateSubjectClassification(trendDto);

        Assert.True(classification.HasEnoughData);
        Assert.Equal(0m, classification.SuccessRate);
        Assert.Equal(0m, classification.Accuracy);
        Assert.Equal(0m, classification.Coverage);
        Assert.Equal(AcademicClassificationState.Critical, classification.State);
        Assert.Equal(ClassificationInsufficiencyReason.None, classification.InsufficiencyReason);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public void CalculateTopicClassification_TopicSufficiencyBounds()
    {
        var topicId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Topic bounds: TopicMinObservations = 2, TopicMinQuestions = 5
        var points = new List<TopicObservationPointDto>
        {
            CreateTopicPoint(topicId, now.AddDays(-2), total: 3, correct: 2),
            CreateTopicPoint(topicId, now.AddDays(-1), total: 3, correct: 2) // 2 obs, 6 questions total
        };

        var trendDto = AcademicTrendCalculator.CalculateTopicTrend(topicId, "Türev", subjectId, "Matematik", points, windowSize: 3);
        var classification = AcademicClassificationCalculator.CalculateTopicClassification(trendDto);

        Assert.True(classification.HasEnoughData);
        Assert.Equal(AcademicClassificationState.Developing, classification.State); // 4/6 = 0.6667 -> Developing
        Assert.Equal(ClassificationInsufficiencyReason.None, classification.InsufficiencyReason);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public void CalculateLearningOutcomeClassification_LOSufficiencyBounds()
    {
        var loId = Guid.NewGuid();
        var topicId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // LO bounds: LOMinObservations = 2, LOMinQuestions = 3
        var points = new List<LearningOutcomeObservationPointDto>
        {
            CreateLOPoint(loId, now.AddDays(-2), total: 2, correct: 1),
            CreateLOPoint(loId, now.AddDays(-1), total: 2, correct: 1) // 2 obs, 4 questions total
        };

        var trendDto = AcademicTrendCalculator.CalculateLearningOutcomeTrend(loId, "MAT.11.1", "Türev Alma", topicId, "Türev", subjectId, "Matematik", points, windowSize: 3);
        var classification = AcademicClassificationCalculator.CalculateLearningOutcomeClassification(trendDto);

        Assert.True(classification.HasEnoughData);
        Assert.Equal(AcademicClassificationState.Weak, classification.State); // 2/4 = 0.5000 -> Weak
        Assert.Equal(ClassificationInsufficiencyReason.None, classification.InsufficiencyReason);
    }

    private static SubjectObservationPointDto CreateSubjectPoint(
        Guid subjectId,
        DateTimeOffset takenAt,
        int total,
        int correct,
        int? wrong = null,
        int? blank = null)
    {
        var w = wrong ?? (total - correct);
        var b = blank ?? 0;
        var net = correct - (w * 0.25m);
        var attempted = correct + w;
        return new SubjectObservationPointDto(
            AttemptId: Guid.NewGuid(),
            ExamId: Guid.NewGuid(),
            ExamTitle: "Deneme Sınavı",
            TakenAt: takenAt,
            AttemptNumber: 1,
            SubjectId: subjectId,
            TotalQuestions: total,
            Correct: correct,
            Wrong: w,
            Blank: b,
            Net: net,
            NetPerQuestion: total > 0 ? Math.Round(net / total, 4) : 0m,
            Accuracy: attempted > 0 ? Math.Round((decimal)correct / attempted, 4) : 0m,
            SuccessRate: total > 0 ? Math.Round((decimal)correct / total, 4) : 0m,
            Coverage: total > 0 ? Math.Round((decimal)attempted / total, 4) : 0m
        );
    }

    private static TopicObservationPointDto CreateTopicPoint(
        Guid topicId,
        DateTimeOffset takenAt,
        int total,
        int correct)
    {
        var wrong = total - correct;
        var net = correct - (wrong * 0.25m);
        return new TopicObservationPointDto(
            AttemptId: Guid.NewGuid(),
            ExamId: Guid.NewGuid(),
            ExamTitle: "Deneme Sınavı",
            TakenAt: takenAt,
            AttemptNumber: 1,
            TopicId: topicId,
            TotalQuestions: total,
            Correct: correct,
            Wrong: wrong,
            Blank: 0,
            Net: net,
            NetPerQuestion: total > 0 ? Math.Round(net / total, 4) : 0m,
            Accuracy: Math.Round((decimal)correct / total, 4),
            SuccessRate: Math.Round((decimal)correct / total, 4),
            Coverage: 1.0000m
        );
    }

    private static LearningOutcomeObservationPointDto CreateLOPoint(
        Guid loId,
        DateTimeOffset takenAt,
        int total,
        int correct)
    {
        var wrong = total - correct;
        return new LearningOutcomeObservationPointDto(
            AttemptId: Guid.NewGuid(),
            ExamId: Guid.NewGuid(),
            ExamTitle: "Deneme Sınavı",
            TakenAt: takenAt,
            AttemptNumber: 1,
            LearningOutcomeId: loId,
            TotalQuestions: total,
            Correct: correct,
            Wrong: wrong,
            Blank: 0,
            Accuracy: Math.Round((decimal)correct / total, 4),
            SuccessRate: Math.Round((decimal)correct / total, 4),
            Coverage: 1.0000m
        );
    }
}
