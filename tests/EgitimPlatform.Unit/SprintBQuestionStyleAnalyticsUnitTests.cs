using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using FluentAssertions;
using Xunit;

namespace EgitimPlatform.Unit;

public class SprintBQuestionStyleAnalyticsUnitTests
{
    private static readonly QuestionStyleMeta StyleNewGen = new(Guid.NewGuid(), "NEW_GENERATION", "Yeni Nesil", 1);
    private static readonly QuestionStyleMeta StyleGraph = new(Guid.NewGuid(), "GRAPH_TABLE", "Grafik / Tablo", 5);
    private static readonly QuestionStyleMeta StyleCalc = new(Guid.NewGuid(), "CALCULATION", "İşlem", 3);
    private static readonly QuestionStyleMeta StyleConcept = new(Guid.NewGuid(), "CONCEPTUAL", "Kavramsal", 2);

    private static RawQuestionAttemptObservation CreateObservation(
        Guid attemptId,
        DateTimeOffset takenAt,
        int attemptNumber,
        Guid questionId,
        IReadOnlyCollection<Guid> styleIds,
        bool? isCorrect,
        bool isBlank,
        decimal net = 0m,
        Guid? examId = null)
    {
        return new RawQuestionAttemptObservation(
            attemptId,
            examId ?? attemptId,
            "Deneme Sınavı",
            takenAt,
            attemptNumber,
            questionId,
            styleIds,
            isCorrect,
            isBlank,
            net
        );
    }

    [Fact]
    public void ZeroEvidence_ReturnsAllActiveStyles_WithNullMetricsAndInsufficientData()
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen, StyleGraph };
        var observations = new List<RawQuestionAttemptObservation>();

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r =>
            r.TotalQuestions == 0 &&
            r.AttemptedQuestions == 0 &&
            r.CorrectCount == 0 &&
            r.IncorrectCount == 0 &&
            r.BlankCount == 0 &&
            r.Accuracy == null &&
            r.SuccessRate == null &&
            r.Coverage == 0m &&
            r.ObservationCount == 0 &&
            r.ClassificationState == AcademicClassificationState.InsufficientData &&
            r.TrendDirection == null
        );
    }

    [Fact]
    public void SingleStyle_CorrectResult_CalculatesCorrectCountsAndMetrics()
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen };
        var attemptId1 = Guid.NewGuid();
        var attemptId2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 3 questions in attempt 1, 2 in attempt 2 -> 5 total questions, 2 attempts (meets minimum evidence)
        var observations = new List<RawQuestionAttemptObservation>
        {
            CreateObservation(attemptId1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(attemptId1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(attemptId1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(attemptId2, now.AddDays(-1), 2, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(attemptId2, now.AddDays(-1), 2, Guid.NewGuid(), [StyleNewGen.Id], true, false),
        };

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        results.Should().HaveCount(1);
        var r = results[0];
        r.TotalQuestions.Should().Be(5);
        r.AttemptedQuestions.Should().Be(5);
        r.CorrectCount.Should().Be(5);
        r.IncorrectCount.Should().Be(0);
        r.BlankCount.Should().Be(0);
        r.Accuracy.Should().Be(1.0000m);
        r.SuccessRate.Should().Be(1.0000m);
        r.Coverage.Should().Be(1.0000m);
        r.ObservationCount.Should().Be(2);
        r.ClassificationState.Should().Be(AcademicClassificationState.Strong);
    }

    [Fact]
    public void SingleStyle_IncorrectResult_CalculatesCorrectly()
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen };
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var observations = new List<RawQuestionAttemptObservation>
        {
            CreateObservation(a1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a2, now.AddDays(-1), 2, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a2, now.AddDays(-1), 2, Guid.NewGuid(), [StyleNewGen.Id], false, false),
        };

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        var r = results.Single();
        r.TotalQuestions.Should().Be(5);
        r.AttemptedQuestions.Should().Be(5);
        r.CorrectCount.Should().Be(0);
        r.IncorrectCount.Should().Be(5);
        r.BlankCount.Should().Be(0);
        r.Accuracy.Should().Be(0.0000m);
        r.SuccessRate.Should().Be(0.0000m);
        r.Coverage.Should().Be(1.0000m);
        r.ClassificationState.Should().Be(AcademicClassificationState.Critical);
    }

    [Fact]
    public void BlankResult_ExcludesFromAccuracyDenominator_AndAffectsCoverage()
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen };
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 5 total: 2 correct, 1 incorrect, 2 blank
        var observations = new List<RawQuestionAttemptObservation>
        {
            CreateObservation(a1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], null, true),
            CreateObservation(a2, now.AddDays(-1), 2, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a2, now.AddDays(-1), 2, Guid.NewGuid(), [StyleNewGen.Id], null, true),
        };

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        var r = results.Single();
        r.TotalQuestions.Should().Be(5);
        r.AttemptedQuestions.Should().Be(3); // 2 correct + 1 incorrect
        r.CorrectCount.Should().Be(2);
        r.IncorrectCount.Should().Be(1);
        r.BlankCount.Should().Be(2);

        // Accuracy = 2 / 3 = 0.6667
        r.Accuracy.Should().Be(0.6667m);
        // SuccessRate = 2 / 5 = 0.4000
        r.SuccessRate.Should().Be(0.4000m);
        // Coverage = 3 / 5 = 0.6000
        r.Coverage.Should().Be(0.6000m);
        // 0.40 <= SR < 0.60 -> Weak
        r.ClassificationState.Should().Be(AcademicClassificationState.Weak);
    }

    [Fact]
    public void MultiStyleQuestion_CountsOneObservationForEveryAssignedStyle()
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen, StyleGraph };
        var a1 = Guid.NewGuid();
        var qId = Guid.NewGuid();

        // 1 question with BOTH styles, answered correctly
        var observations = new List<RawQuestionAttemptObservation>
        {
            CreateObservation(a1, DateTimeOffset.UtcNow, 1, qId, [StyleNewGen.Id, StyleGraph.Id], true, false)
        };

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        results.Should().HaveCount(2);
        var newGen = results.Single(r => r.QuestionStyleId == StyleNewGen.Id);
        var graph = results.Single(r => r.QuestionStyleId == StyleGraph.Id);

        newGen.TotalQuestions.Should().Be(1);
        newGen.CorrectCount.Should().Be(1);
        newGen.AttemptedQuestions.Should().Be(1);

        graph.TotalQuestions.Should().Be(1);
        graph.CorrectCount.Should().Be(1);
        graph.AttemptedQuestions.Should().Be(1);
    }

    [Fact]
    public void ZeroStyleQuestion_IsIgnoredForStyleAnalytics()
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen };
        var a1 = Guid.NewGuid();

        // Question with zero styles
        var observations = new List<RawQuestionAttemptObservation>
        {
            CreateObservation(a1, DateTimeOffset.UtcNow, 1, Guid.NewGuid(), [], true, false)
        };

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        var r = results.Single();
        r.TotalQuestions.Should().Be(0);
        r.ClassificationState.Should().Be(AcademicClassificationState.InsufficientData);
    }

    [Fact]
    public void InsufficientData_WhenObservationsLessThanTwo_EvenIfTotalQuestionsHigh()
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen };
        var a1 = Guid.NewGuid(); // Only 1 attempt

        var observations = new List<RawQuestionAttemptObservation>();
        for (int i = 0; i < 10; i++)
        {
            observations.Add(CreateObservation(a1, DateTimeOffset.UtcNow, 1, Guid.NewGuid(), [StyleNewGen.Id], true, false));
        }

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        var r = results.Single();
        r.TotalQuestions.Should().Be(10);
        r.ObservationCount.Should().Be(1);
        r.ClassificationState.Should().Be(AcademicClassificationState.InsufficientData);
    }

    [Fact]
    public void InsufficientData_WhenTotalQuestionsLessThanFive_EvenIfObservationsSufficient()
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen };
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // 2 attempts, but only 4 questions total
        var observations = new List<RawQuestionAttemptObservation>
        {
            CreateObservation(a1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a1, now.AddDays(-2), 1, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a2, now.AddDays(-1), 2, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a2, now.AddDays(-1), 2, Guid.NewGuid(), [StyleNewGen.Id], true, false),
        };

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        var r = results.Single();
        r.TotalQuestions.Should().Be(4);
        r.ObservationCount.Should().Be(2);
        r.ClassificationState.Should().Be(AcademicClassificationState.InsufficientData);
    }

    [Theory]
    [InlineData(1, 5, AcademicClassificationState.Critical)]   // 1/5 = 0.20 < 0.40
    [InlineData(2, 5, AcademicClassificationState.Weak)]       // 2/5 = 0.40 -> Weak
    [InlineData(3, 5, AcademicClassificationState.Developing)] // 3/5 = 0.60 -> Developing
    [InlineData(4, 5, AcademicClassificationState.Strong)]     // 4/5 = 0.80 -> Strong
    public void ClassificationThresholds_ProduceCorrectStates(int correct, int total, AcademicClassificationState expected)
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen };
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var observations = new List<RawQuestionAttemptObservation>();
        for (int i = 0; i < total; i++)
        {
            var attemptId = i % 2 == 0 ? a1 : a2;
            var isCorrect = i < correct;
            observations.Add(CreateObservation(attemptId, now.AddDays(-i), i, Guid.NewGuid(), [StyleNewGen.Id], isCorrect, false));
        }

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        var r = results.Single();
        r.ClassificationState.Should().Be(expected);
    }

    [Fact]
    public void DeterministicSorting_CriticalFirst_ThenWeak_Developing_Strong_InsufficientData_ThenLowestSuccessRate()
    {
        // 4 active styles
        var sCritical = new QuestionStyleMeta(Guid.NewGuid(), "CRITICAL_STYLE", "Kritik Tarz", 1);
        var sWeak = new QuestionStyleMeta(Guid.NewGuid(), "WEAK_STYLE", "Zayıf Tarz", 2);
        var sDevLow = new QuestionStyleMeta(Guid.NewGuid(), "DEV_LOW", "Gelişen Düşük", 3);
        var sDevHigh = new QuestionStyleMeta(Guid.NewGuid(), "DEV_HIGH", "Gelişen Yüksek", 4);
        var sStrong = new QuestionStyleMeta(Guid.NewGuid(), "STRONG_STYLE", "Güçlü Tarz", 5);
        var sNoData = new QuestionStyleMeta(Guid.NewGuid(), "NO_DATA", "Verisiz Tarz", 6);

        var activeStyles = new List<QuestionStyleMeta> { sStrong, sNoData, sDevHigh, sWeak, sCritical, sDevLow };

        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var observations = new List<RawQuestionAttemptObservation>();

        // Critical: 1/5 = 0.20
        for (int i = 0; i < 5; i++)
            observations.Add(CreateObservation(i % 2 == 0 ? a1 : a2, now.AddDays(-i), i, Guid.NewGuid(), [sCritical.Id], i < 1, false));

        // Weak: 2/5 = 0.40
        for (int i = 0; i < 5; i++)
            observations.Add(CreateObservation(i % 2 == 0 ? a1 : a2, now.AddDays(-i), i, Guid.NewGuid(), [sWeak.Id], i < 2, false));

        // Developing Low: 3/5 = 0.60
        for (int i = 0; i < 5; i++)
            observations.Add(CreateObservation(i % 2 == 0 ? a1 : a2, now.AddDays(-i), i, Guid.NewGuid(), [sDevLow.Id], i < 3, false));

        // Developing High: 7/10 = 0.70
        for (int i = 0; i < 10; i++)
            observations.Add(CreateObservation(i % 2 == 0 ? a1 : a2, now.AddDays(-i), i, Guid.NewGuid(), [sDevHigh.Id], i < 7, false));

        // Strong: 5/5 = 1.00
        for (int i = 0; i < 5; i++)
            observations.Add(CreateObservation(i % 2 == 0 ? a1 : a2, now.AddDays(-i), i, Guid.NewGuid(), [sStrong.Id], true, false));

        var results = QuestionStyleAnalyticsCalculator.CalculateAnalytics(activeStyles, observations);

        results.Select(r => r.Code).Should().ContainInOrder(
            "CRITICAL_STYLE",
            "WEAK_STYLE",
            "DEV_LOW",
            "DEV_HIGH",
            "STRONG_STYLE",
            "NO_DATA"
        );
    }

    [Fact]
    public void StyleTrend_Increasing_Stable_Decreasing_EvaluatedCorrectly()
    {
        var activeStyles = new List<QuestionStyleMeta> { StyleNewGen };
        var now = DateTimeOffset.UtcNow;

        // 4 attempts (windowSize = 2 -> prev = [a1, a2], rec = [a3, a4])
        // Case 1: Increasing: prev = 0/4 (0%), rec = 4/4 (100%) -> delta = +1.00
        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();
        var a3 = Guid.NewGuid();
        var a4 = Guid.NewGuid();

        var incObs = new List<RawQuestionAttemptObservation>
        {
            CreateObservation(a1, now.AddDays(-4), 1, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a1, now.AddDays(-4), 1, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a2, now.AddDays(-3), 2, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a2, now.AddDays(-3), 2, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a3, now.AddDays(-2), 3, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a3, now.AddDays(-2), 3, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a4, now.AddDays(-1), 4, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a4, now.AddDays(-1), 4, Guid.NewGuid(), [StyleNewGen.Id], true, false),
        };

        var incTrends = QuestionStyleAnalyticsCalculator.CalculateTrends(activeStyles, incObs, windowSize: 2);
        incTrends.Single().Direction.Should().Be(TrendDirection.Increasing);
        incTrends.Single().HasEnoughData.Should().BeTrue();

        // Case 2: Decreasing: prev = 4/4 (100%), rec = 0/4 (0%) -> delta = -1.00
        var decObs = new List<RawQuestionAttemptObservation>
        {
            CreateObservation(a1, now.AddDays(-4), 1, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a1, now.AddDays(-4), 1, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a2, now.AddDays(-3), 2, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a2, now.AddDays(-3), 2, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a3, now.AddDays(-2), 3, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a3, now.AddDays(-2), 3, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a4, now.AddDays(-1), 4, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a4, now.AddDays(-1), 4, Guid.NewGuid(), [StyleNewGen.Id], false, false),
        };

        var decTrends = QuestionStyleAnalyticsCalculator.CalculateTrends(activeStyles, decObs, windowSize: 2);
        decTrends.Single().Direction.Should().Be(TrendDirection.Decreasing);

        // Case 3: Stable: prev = 2/4 (50%), rec = 2/4 (50%) -> delta = 0.00
        var stObs = new List<RawQuestionAttemptObservation>
        {
            CreateObservation(a1, now.AddDays(-4), 1, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a1, now.AddDays(-4), 1, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a2, now.AddDays(-3), 2, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a2, now.AddDays(-3), 2, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a3, now.AddDays(-2), 3, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a3, now.AddDays(-2), 3, Guid.NewGuid(), [StyleNewGen.Id], false, false),
            CreateObservation(a4, now.AddDays(-1), 4, Guid.NewGuid(), [StyleNewGen.Id], true, false),
            CreateObservation(a4, now.AddDays(-1), 4, Guid.NewGuid(), [StyleNewGen.Id], false, false),
        };

        var stTrends = QuestionStyleAnalyticsCalculator.CalculateTrends(activeStyles, stObs, windowSize: 2);
        stTrends.Single().Direction.Should().Be(TrendDirection.Stable);
    }
}
