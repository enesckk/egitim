using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint6FStudyPlanUnitTests
{
    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void CalculateStudyPlan_NoRecommendations_ReturnsEmptyPlanWithFullUnallocatedMinutes()
    {
        var studentId = Guid.NewGuid();
        var plan = AcademicStudyPlanCalculator.CalculateStudyPlan(
            studentId,
            [],
            planDays: 7,
            dailyStudyMinutes: 120
        );

        Assert.Equal(7, plan.PlanDays);
        Assert.Equal(120, plan.DailyStudyMinutes);
        Assert.Equal(840, plan.TotalAvailableMinutes);
        Assert.Equal(0, plan.TotalPlannedMinutes);
        Assert.Equal(840, plan.UnallocatedMinutes);
        Assert.Equal(0m, plan.UtilizationRate);
        Assert.Equal(0, plan.ScheduledRecommendationCount);
        Assert.Equal(0, plan.UnscheduledRecommendationCount);
        Assert.Equal(7, plan.Days.Count);
        Assert.All(plan.Days, d => Assert.Empty(d.Sessions));
    }

    [Fact]
    public void CalculateStudyPlan_CriticalOverWeak_CriticalGetsHigherOrEqualAllocation()
    {
        var studentId = Guid.NewGuid();
        var criticalRec = new AcademicRecommendationDto(
            AcademicRecommendationType.RemediateCritical,
            RecommendationNodeType.LearningOutcome,
            Guid.NewGuid(),
            "LO-CRIT",
            "Critical LO",
            Guid.NewGuid(), "Math", Guid.NewGuid(), "Alg", Guid.NewGuid(), "Sub", Guid.NewGuid(), "Critical LO",
            AcademicClassificationState.Critical,
            ClassificationBasis.RecentWindow,
            0.10m, 0.10m, 0.50m, TrendDirection.Decreasing, -0.20m, 5, 20, 95, "v1", "v1", false
        );

        var weakRec = new AcademicRecommendationDto(
            AcademicRecommendationType.StrengthenWeak,
            RecommendationNodeType.LearningOutcome,
            Guid.NewGuid(),
            "LO-WEAK",
            "Weak LO",
            Guid.NewGuid(), "Math", Guid.NewGuid(), "Alg", Guid.NewGuid(), "Sub", Guid.NewGuid(), "Weak LO",
            AcademicClassificationState.Weak,
            ClassificationBasis.RecentWindow,
            0.45m, 0.45m, 0.50m, TrendDirection.Stable, 0.00m, 5, 20, 55, "v1", "v1", false
        );

        var plan = AcademicStudyPlanCalculator.CalculateStudyPlan(
            studentId,
            [criticalRec, weakRec],
            planDays: 7,
            dailyStudyMinutes: 120
        );

        int criticalSessions = plan.Days.SelectMany(d => d.Sessions).Count(s => s.NodeId == criticalRec.NodeId);
        int weakSessions = plan.Days.SelectMany(d => d.Sessions).Count(s => s.NodeId == weakRec.NodeId);

        Assert.True(criticalSessions >= weakSessions, $"Critical ({criticalSessions}) should receive >= Weak ({weakSessions}) sessions.");
    }

    [Fact]
    public void CalculateStudyPlan_MaxShareGuard_LimitsSingleCandidateTo40PercentCapacity()
    {
        var studentId = Guid.NewGuid();
        var singleCritRec = new AcademicRecommendationDto(
            AcademicRecommendationType.RemediateCritical,
            RecommendationNodeType.LearningOutcome,
            Guid.NewGuid(),
            "LO-SINGLE",
            "Single Critical LO",
            Guid.NewGuid(), "Math", Guid.NewGuid(), "Alg", Guid.NewGuid(), "Sub", Guid.NewGuid(), "Single Critical LO",
            AcademicClassificationState.Critical,
            ClassificationBasis.RecentWindow,
            0.00m, 0.00m, 0.50m, TrendDirection.Decreasing, -0.30m, 5, 20, 100, "v1", "v1", false
        );

        // 7 days * 120 mins = 840 mins = 28 sessions total
        var plan = AcademicStudyPlanCalculator.CalculateStudyPlan(
            studentId,
            [singleCritRec],
            planDays: 7,
            dailyStudyMinutes: 120
        );

        int allocatedSessions = plan.Days.SelectMany(d => d.Sessions).Count(s => s.NodeId == singleCritRec.NodeId);
        int maxAllowed = (int)Math.Floor(28 * 0.40m); // 11 sessions

        Assert.True(allocatedSessions <= maxAllowed, $"Allocated sessions ({allocatedSessions}) must not exceed 40% cap ({maxAllowed}).");
        Assert.Equal(allocatedSessions * 30, plan.TotalPlannedMinutes);
        Assert.Equal(840 - (allocatedSessions * 30), plan.UnallocatedMinutes);
    }

    [Fact]
    public void CalculateStudyPlan_DayDistribution_SpreadsMultiSessionItemAcrossDays()
    {
        var studentId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        var rec1 = new AcademicRecommendationDto(
            AcademicRecommendationType.RemediateCritical,
            RecommendationNodeType.LearningOutcome,
            Guid.NewGuid(), "LO-1", "LO One",
            Guid.NewGuid(), "Math", Guid.NewGuid(), "Alg", Guid.NewGuid(), "Sub", Guid.NewGuid(), "LO One",
            AcademicClassificationState.Critical, ClassificationBasis.RecentWindow,
            0.10m, 0.10m, 0.50m, TrendDirection.Decreasing, -0.20m, 5, 20, 90, "v1", "v1", false
        );

        var rec2 = new AcademicRecommendationDto(
            AcademicRecommendationType.RemediateCritical,
            RecommendationNodeType.LearningOutcome,
            Guid.NewGuid(), "LO-2", "LO Two",
            Guid.NewGuid(), "Math", Guid.NewGuid(), "Alg", Guid.NewGuid(), "Sub", Guid.NewGuid(), "LO Two",
            AcademicClassificationState.Critical, ClassificationBasis.RecentWindow,
            0.15m, 0.15m, 0.50m, TrendDirection.Stable, 0.00m, 5, 20, 85, "v1", "v1", false
        );

        var plan = AcademicStudyPlanCalculator.CalculateStudyPlan(
            studentId,
            [rec1, rec2],
            planDays: 5,
            dailyStudyMinutes: 60, // 2 sessions per day capacity, 10 sessions total
            startDate: startDate
        );

        Assert.Equal(5, plan.Days.Count);
        Assert.Equal(startDate, plan.Days[0].Date);
        Assert.Equal(startDate.AddDays(4), plan.Days[4].Date);

        // Ensure no day exceeds daily capacity (2 sessions = 60 mins)
        Assert.All(plan.Days, d => Assert.True(d.Sessions.Count <= 2));

        // Check sequence ordering within each day
        foreach (var day in plan.Days)
        {
            for (int i = 0; i < day.Sessions.Count; i++)
            {
                Assert.Equal(i + 1, day.Sessions[i].Sequence);
                Assert.Equal(30, day.Sessions[i].DurationMinutes);
            }
        }
    }

    [Fact]
    public void CalculateStudyPlan_NonDivisibleDailyMinutes_DoesNotExceedSchedulableDailyCapacity()
    {
        var studentId = Guid.NewGuid();
        // 7 days * 100 mins/day = 700 available mins.
        // DailySessionCapacity = floor(100 / 30) = 3 sessions/day.
        // Schedulable total capacity = 3 * 7 = 21 sessions (630 mins max).
        // 70 mins remain unusable due to session duration (30 min) granularity.
        List<AcademicRecommendationDto> recs = [];
        for (int i = 1; i <= 30; i++)
        {
            recs.Add(new AcademicRecommendationDto(
                AcademicRecommendationType.RemediateCritical,
                RecommendationNodeType.LearningOutcome,
                Guid.NewGuid(), $"LO-{i}", $"LO {i}",
                Guid.NewGuid(), "Math", Guid.NewGuid(), "Alg", Guid.NewGuid(), "Sub", Guid.NewGuid(), $"LO {i}",
                AcademicClassificationState.Critical, ClassificationBasis.RecentWindow,
                0.10m, 0.10m, 0.50m, TrendDirection.Decreasing, -0.20m, 5, 20, 100 - i, "v1", "v1", false
            ));
        }

        var plan = AcademicStudyPlanCalculator.CalculateStudyPlan(
            studentId,
            recs,
            planDays: 7,
            dailyStudyMinutes: 100
        );

        Assert.Equal(700, plan.TotalAvailableMinutes);
        Assert.Equal(630, plan.TotalPlannedMinutes); // 21 sessions * 30 mins
        Assert.Equal(70, plan.UnallocatedMinutes);  // 700 - 630 = 70 mins unallocated
        Assert.Equal(21, plan.Days.Sum(d => d.Sessions.Count));

        // Check day capacity invariant
        Assert.All(plan.Days, d =>
        {
            Assert.True(d.Sessions.Count <= 3);
            Assert.True(d.PlannedMinutes <= 90);
            Assert.True(d.PlannedMinutes <= 100);
        });

        // Conservation invariant
        Assert.Equal(plan.TotalPlannedMinutes, plan.Days.Sum(d => d.PlannedMinutes));
    }

    [Fact]
    public void CalculateStudyPlan_NonDivisible3Days50Mins_CalculatesCorrectUnallocatedFloor()
    {
        var studentId = Guid.NewGuid();
        // 3 days * 50 mins/day = 150 available mins.
        // DailySessionCapacity = floor(50 / 30) = 1 session/day.
        // Schedulable total capacity = 1 * 3 = 3 sessions (90 mins max).
        // 60 mins unallocated floor.
        List<AcademicRecommendationDto> recs = [
            new(AcademicRecommendationType.RemediateCritical, RecommendationNodeType.LearningOutcome, Guid.NewGuid(), "L1", "L1", Guid.NewGuid(), "M", Guid.NewGuid(), "A", Guid.NewGuid(), "S", Guid.NewGuid(), "L1", AcademicClassificationState.Critical, ClassificationBasis.RecentWindow, 0m, 0m, 0.5m, TrendDirection.Decreasing, 0m, 5, 20, 90, "v1", "v1", false),
            new(AcademicRecommendationType.RemediateCritical, RecommendationNodeType.LearningOutcome, Guid.NewGuid(), "L2", "L2", Guid.NewGuid(), "M", Guid.NewGuid(), "A", Guid.NewGuid(), "S", Guid.NewGuid(), "L2", AcademicClassificationState.Critical, ClassificationBasis.RecentWindow, 0m, 0m, 0.5m, TrendDirection.Decreasing, 0m, 5, 20, 80, "v1", "v1", false),
            new(AcademicRecommendationType.RemediateCritical, RecommendationNodeType.LearningOutcome, Guid.NewGuid(), "L3", "L3", Guid.NewGuid(), "M", Guid.NewGuid(), "A", Guid.NewGuid(), "S", Guid.NewGuid(), "L3", AcademicClassificationState.Critical, ClassificationBasis.RecentWindow, 0m, 0m, 0.5m, TrendDirection.Decreasing, 0m, 5, 20, 70, "v1", "v1", false)
        ];

        var plan = AcademicStudyPlanCalculator.CalculateStudyPlan(
            studentId,
            recs,
            planDays: 3,
            dailyStudyMinutes: 50
        );

        Assert.Equal(150, plan.TotalAvailableMinutes);
        Assert.Equal(90, plan.TotalPlannedMinutes);
        Assert.Equal(60, plan.UnallocatedMinutes);
        Assert.All(plan.Days, d => Assert.True(d.Sessions.Count <= 1));
    }

    [Fact]
    public async Task GetStudyPlan_Validation_ThrowsExceptionOnInvalidContractInput()
    {
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var recHandler = Substitute.ForPartsOf<GetStudentAcademicRecommendationHandler>(resourceAccess, Substitute.For<GetStudentKnowledgeMapHandler>(Db(), resourceAccess, Substitute.For<GetStudentAcademicClassificationHandler>(Substitute.For<GetStudentAcademicTrendHandler>(Db(), Substitute.For<IAcademicCatalog>(), resourceAccess))));
        var studyPlanHandler = new GetStudentAcademicStudyPlanHandler(resourceAccess, recHandler);

        var invalidDaysQuery = new AcademicStudyPlanQuery(Guid.NewGuid(), PlanDays: 15);
        await Assert.ThrowsAsync<ValidationException>(() => studyPlanHandler.GetStudyPlanAsync(invalidDaysQuery));

        var invalidMinutesQuery = new AcademicStudyPlanQuery(Guid.NewGuid(), DailyStudyMinutes: 20);
        await Assert.ThrowsAsync<ValidationException>(() => studyPlanHandler.GetStudyPlanAsync(invalidMinutesQuery));
    }
}
