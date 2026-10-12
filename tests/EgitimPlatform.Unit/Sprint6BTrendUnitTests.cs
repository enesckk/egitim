using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint6BTrendUnitTests
{
    [Fact]
    public void CalculateSubjectTrend_Increasing_ReturnsIncreasingDirection()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var points = new List<SubjectObservationPointDto>
        {
            // Previous Window (1, 2, 3): 50% success rate
            CreatePoint(subjectId, now.AddDays(-6), total: 10, correct: 5),
            CreatePoint(subjectId, now.AddDays(-5), total: 10, correct: 5),
            CreatePoint(subjectId, now.AddDays(-4), total: 10, correct: 5),
            // Recent Window (4, 5, 6): 80% success rate
            CreatePoint(subjectId, now.AddDays(-3), total: 10, correct: 8),
            CreatePoint(subjectId, now.AddDays(-2), total: 10, correct: 8),
            CreatePoint(subjectId, now.AddDays(-1), total: 10, correct: 8)
        };

        var trend = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Matematik", points, windowSize: 3);

        Assert.True(trend.HasEnoughData);
        Assert.NotNull(trend.PreviousWindow);
        Assert.NotNull(trend.RecentWindow);
        Assert.Equal(0.5000m, trend.PreviousWindow.SuccessRate);
        Assert.Equal(0.8000m, trend.RecentWindow.SuccessRate);
        Assert.Equal(0.3000m, trend.SuccessRateDelta);
        Assert.Equal(TrendDirection.Increasing, trend.Direction);
    }

    [Fact]
    public void CalculateSubjectTrend_Decreasing_ReturnsDecreasingDirection()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var points = new List<SubjectObservationPointDto>
        {
            // Previous Window: 80% success rate
            CreatePoint(subjectId, now.AddDays(-6), total: 10, correct: 8),
            CreatePoint(subjectId, now.AddDays(-5), total: 10, correct: 8),
            CreatePoint(subjectId, now.AddDays(-4), total: 10, correct: 8),
            // Recent Window: 40% success rate
            CreatePoint(subjectId, now.AddDays(-3), total: 10, correct: 4),
            CreatePoint(subjectId, now.AddDays(-2), total: 10, correct: 4),
            CreatePoint(subjectId, now.AddDays(-1), total: 10, correct: 4)
        };

        var trend = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Fizik", points, windowSize: 3);

        Assert.True(trend.HasEnoughData);
        Assert.Equal(-0.4000m, trend.SuccessRateDelta);
        Assert.Equal(TrendDirection.Decreasing, trend.Direction);
    }

    [Fact]
    public void CalculateSubjectTrend_Stable_ReturnsStableDirectionWhenWithinEpsilon()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var points = new List<SubjectObservationPointDto>
        {
            // Previous Window: 60% success rate
            CreatePoint(subjectId, now.AddDays(-6), total: 10, correct: 6),
            CreatePoint(subjectId, now.AddDays(-5), total: 10, correct: 6),
            CreatePoint(subjectId, now.AddDays(-4), total: 10, correct: 6),
            // Recent Window: 62% success rate (Delta = +0.02, <= epsilon 0.03)
            CreatePoint(subjectId, now.AddDays(-3), total: 100, correct: 62),
            CreatePoint(subjectId, now.AddDays(-2), total: 100, correct: 62),
            CreatePoint(subjectId, now.AddDays(-1), total: 100, correct: 62)
        };

        var trend = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Kimya", points, windowSize: 3);

        Assert.True(trend.HasEnoughData);
        Assert.Equal(0.0200m, trend.SuccessRateDelta);
        Assert.Equal(TrendDirection.Stable, trend.Direction);
    }

    [Fact]
    public void CalculateSubjectTrend_AdjacentWindows_SelectsLast6Of10Observations()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 10 observations chronologically
        var points = new List<SubjectObservationPointDto>();
        for (int i = 1; i <= 10; i++)
        {
            // Points 1..4: 30% success, 5..7: 50% success, 8..10: 90% success
            int correct = i <= 4 ? 3 : (i <= 7 ? 5 : 9);
            points.Add(CreatePoint(subjectId, now.AddDays(-11 + i), total: 10, correct: correct));
        }

        var trend = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Geometri", points, windowSize: 3);

        Assert.True(trend.HasEnoughData);
        Assert.Equal(10, trend.TotalObservations);
        Assert.NotNull(trend.PreviousWindow);
        Assert.NotNull(trend.RecentWindow);

        // Previous Window must be points 5, 6, 7 (50% success)
        Assert.Equal(0.5000m, trend.PreviousWindow.SuccessRate);
        // Recent Window must be points 8, 9, 10 (90% success)
        Assert.Equal(0.9000m, trend.RecentWindow.SuccessRate);
        Assert.Equal(0.4000m, trend.SuccessRateDelta);
        Assert.Equal(TrendDirection.Increasing, trend.Direction);
    }

    [Fact]
    public void CalculateSubjectTrend_InsufficientData_ReturnsHasEnoughDataFalse()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Only 5 points for windowSize = 3 (requires 6)
        var points = new List<SubjectObservationPointDto>
        {
            CreatePoint(subjectId, now.AddDays(-5), total: 10, correct: 5),
            CreatePoint(subjectId, now.AddDays(-4), total: 10, correct: 5),
            CreatePoint(subjectId, now.AddDays(-3), total: 10, correct: 5),
            CreatePoint(subjectId, now.AddDays(-2), total: 10, correct: 8),
            CreatePoint(subjectId, now.AddDays(-1), total: 10, correct: 8)
        };

        var trend = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Biyoloji", points, windowSize: 3);

        Assert.False(trend.HasEnoughData);
        Assert.Null(trend.PreviousWindow);
        Assert.Null(trend.RecentWindow);
        Assert.Null(trend.SuccessRateDelta);
        Assert.Null(trend.Direction);
        Assert.Equal(5, trend.TotalObservations);
        Assert.Equal(5, trend.TimeSeries.Count);
    }

    [Fact]
    public void CalculateWindowMetrics_CountWeighted_WeightsLargeExamsMoreThanSmallExams()
    {
        var subjectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 1-question exam (100% success) vs 40-question exam (50% success)
        var points = new List<SubjectObservationPointDto>
        {
            CreatePoint(subjectId, now.AddDays(-2), total: 1, correct: 1),
            CreatePoint(subjectId, now.AddDays(-1), total: 40, correct: 20)
        };

        var metrics = AcademicTrendCalculator.CalculateWindowMetrics(
            observationCount: points.Count,
            totalQuestions: points.Sum(p => p.TotalQuestions),
            correct: points.Sum(p => p.Correct),
            wrong: points.Sum(p => p.Wrong),
            blank: points.Sum(p => p.Blank),
            totalNet: points.Sum(p => p.Net)
        );

        Assert.Equal(0.5122m, metrics.SuccessRate);
    }

    [Fact]
    public void CalculateWindowMetrics_AllBlank_DoesNotThrowDivideByZero()
    {
        var metrics = AcademicTrendCalculator.CalculateWindowMetrics(
            observationCount: 3,
            totalQuestions: 30,
            correct: 0,
            wrong: 0,
            blank: 30,
            totalNet: 0m
        );

        Assert.Equal(0m, metrics.Accuracy);
        Assert.Equal(0m, metrics.SuccessRate);
        Assert.Equal(0m, metrics.Coverage);
        Assert.Equal(0m, metrics.NetPerQuestion);
    }

    [Fact]
    public void CalculateSubjectTrend_Retakes_CountAsSeparateObservationsInChronology()
    {
        var subjectId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var points = new List<SubjectObservationPointDto>
        {
            CreatePoint(subjectId, now.AddDays(-6), total: 10, correct: 4, examId: examId, attemptNum: 1),
            CreatePoint(subjectId, now.AddDays(-5), total: 10, correct: 5, examId: examId, attemptNum: 2),
            CreatePoint(subjectId, now.AddDays(-4), total: 10, correct: 6),
            CreatePoint(subjectId, now.AddDays(-3), total: 10, correct: 7),
            CreatePoint(subjectId, now.AddDays(-2), total: 10, correct: 8),
            CreatePoint(subjectId, now.AddDays(-1), total: 10, correct: 9)
        };

        var trend = AcademicTrendCalculator.CalculateSubjectTrend(subjectId, "Tarih", points, windowSize: 3);

        Assert.True(trend.HasEnoughData);
        Assert.Equal(6, trend.TotalObservations);
        Assert.Equal(1, trend.TimeSeries[0].AttemptNumber);
        Assert.Equal(2, trend.TimeSeries[1].AttemptNumber);
    }

    private static SubjectObservationPointDto CreatePoint(
        Guid subjectId,
        DateTimeOffset takenAt,
        int total,
        int correct,
        Guid? examId = null,
        int attemptNum = 1)
    {
        var wrong = total - correct;
        return new SubjectObservationPointDto(
            AttemptId: Guid.NewGuid(),
            ExamId: examId ?? Guid.NewGuid(),
            ExamTitle: "Deneme Sınavı",
            TakenAt: takenAt,
            AttemptNumber: attemptNum,
            SubjectId: subjectId,
            TotalQuestions: total,
            Correct: correct,
            Wrong: wrong,
            Blank: 0,
            Net: correct - (wrong * 0.25m),
            NetPerQuestion: Math.Round((correct - (wrong * 0.25m)) / total, 4),
            Accuracy: Math.Round((decimal)correct / total, 4),
            SuccessRate: Math.Round((decimal)correct / total, 4),
            Coverage: 1.0000m
        );
    }
}
