namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public static class AcademicStudyPlanCalculator
{
    public static AcademicStudyPlanDto CalculateStudyPlan(
        Guid studentId,
        List<AcademicRecommendationDto> recommendations,
        int planDays,
        int dailyStudyMinutes,
        DateTimeOffset? startDate = null)
    {
        int clampedPlanDays = Math.Clamp(planDays, AcademicStudyPlanPolicy.MinPlanDays, AcademicStudyPlanPolicy.MaxPlanDays);
        int clampedDailyMinutes = Math.Clamp(dailyStudyMinutes, AcademicStudyPlanPolicy.MinDailyStudyMinutes, AcademicStudyPlanPolicy.MaxDailyStudyMinutes);

        int totalAvailableMinutes = clampedPlanDays * clampedDailyMinutes;
        int sessionDuration = AcademicStudyPlanPolicy.SessionMinutes;
        int dailySessionCapacity = clampedDailyMinutes / sessionDuration;
        int totalSessionCapacity = dailySessionCapacity * clampedPlanDays;

        // Filter eligible candidates (Critical, Weak, Developing)
        var eligibleCandidates = recommendations
            .Where(r => r.RecommendationType is AcademicRecommendationType.RemediateCritical
                     or AcademicRecommendationType.StrengthenWeak
                     or AcademicRecommendationType.ReinforceDeveloping)
            .ToList();

        if (totalSessionCapacity == 0 || eligibleCandidates.Count == 0)
        {
            var emptyDays = Enumerable.Range(1, clampedPlanDays).Select(d => new StudyPlanDayDto(
                d,
                startDate?.AddDays(d - 1),
                0,
                0,
                []
            )).ToList();

            return new AcademicStudyPlanDto(
                studentId,
                clampedPlanDays,
                clampedDailyMinutes,
                sessionDuration,
                totalAvailableMinutes,
                0,
                totalAvailableMinutes,
                0m,
                0,
                recommendations.Count,
                AcademicRecommendationPolicy.CurrentRecommendationPolicyVersion,
                AcademicStudyPlanPolicy.CurrentStudyPlanPolicyVersion,
                emptyDays,
                recommendations
            );
        }

        // Max sessions per candidate guard (40% of total session capacity if capacity >= 3)
        int maxSessionsPerCandidate = totalSessionCapacity >= 3
            ? Math.Max(1, (int)Math.Floor(totalSessionCapacity * AcademicStudyPlanPolicy.MaxShareRatio))
            : totalSessionCapacity;

        // Round 1: Select top candidates (up to totalSessionCapacity) and assign 1 base session
        var selectedCandidates = eligibleCandidates.Take(totalSessionCapacity).ToList();
        var candidateAllocations = selectedCandidates.ToDictionary(c => c, _ => 1);
        int remainingSessions = totalSessionCapacity - selectedCandidates.Count;

        // Round 2: Proportional allocation of remaining sessions by PriorityScore
        if (remainingSessions > 0 && selectedCandidates.Count > 0)
        {
            int totalPrioritySum = selectedCandidates.Sum(c => c.PriorityScore);
            if (totalPrioritySum > 0)
            {
                foreach (var candidate in selectedCandidates)
                {
                    if (remainingSessions <= 0) break;

                    int extra = (int)Math.Floor(remainingSessions * ((decimal)candidate.PriorityScore / totalPrioritySum));
                    int maxAllowedExtra = maxSessionsPerCandidate - candidateAllocations[candidate];
                    int extraToAssign = Math.Min(extra, Math.Max(0, maxAllowedExtra));

                    if (extraToAssign > 0)
                    {
                        candidateAllocations[candidate] += extraToAssign;
                        remainingSessions -= extraToAssign;
                    }
                }
            }
        }

        // Round 3: Remainder allocation in priority order up to maxSessionsPerCandidate
        while (remainingSessions > 0)
        {
            var candidateToIncrement = selectedCandidates
                .FirstOrDefault(c => candidateAllocations[c] < maxSessionsPerCandidate);

            if (candidateToIncrement == null)
                break; // All candidates hit max share guard

            candidateAllocations[candidateToIncrement]++;
            remainingSessions--;
        }

        // Unscheduled recommendations
        var scheduledCandidateNodeIds = candidateAllocations.Keys.Select(k => k.NodeId).ToHashSet();
        var unscheduledRecommendations = recommendations
            .Where(r => !scheduledCandidateNodeIds.Contains(r.NodeId))
            .ToList();

        // Day Distribution Algorithm (Spaced Repetition & Daily Session Capacity)
        var dayDtos = Enumerable.Range(1, clampedPlanDays).Select(d => new DayWork(
            d,
            startDate?.AddDays(d - 1),
            dailySessionCapacity
        )).ToList();

        // Expand allocated candidate sessions into individual items
        List<AcademicRecommendationDto> sessionItems = [];
        foreach (var (candidate, count) in candidateAllocations)
        {
            for (int i = 0; i < count; i++)
            {
                sessionItems.Add(candidate);
            }
        }

        // Distribute session items across days trying to avoid same-day duplicates
        int roundRobinIndex = 0;
        foreach (var item in sessionItems)
        {
            DayWork? targetDay = null;

            // Try to find next day in round-robin that has capacity and no duplicate NodeId
            for (int offset = 0; offset < clampedPlanDays; offset++)
            {
                int inspectIdx = (roundRobinIndex + offset) % clampedPlanDays;
                var candidateDay = dayDtos[inspectIdx];
                if (candidateDay.Sessions.Count < candidateDay.Capacity && !candidateDay.NodeIds.Contains(item.NodeId))
                {
                    targetDay = candidateDay;
                    roundRobinIndex = inspectIdx + 1;
                    break;
                }
            }

            // Fallback 1: Any day with remaining capacity
            if (targetDay == null)
            {
                for (int offset = 0; offset < clampedPlanDays; offset++)
                {
                    int inspectIdx = (roundRobinIndex + offset) % clampedPlanDays;
                    var candidateDay = dayDtos[inspectIdx];
                    if (candidateDay.Sessions.Count < candidateDay.Capacity)
                    {
                        targetDay = candidateDay;
                        roundRobinIndex = inspectIdx + 1;
                        break;
                    }
                }
            }

            // Fallback 2: Day with minimum current sessions
            if (targetDay == null)
            {
                targetDay = dayDtos.OrderBy(d => d.Sessions.Count).First();
            }

            targetDay.NodeIds.Add(item.NodeId);
            targetDay.Sessions.Add(item);
        }

        // Build final Day DTOs
        var finalDays = dayDtos.Select(d =>
        {
            var sessions = d.Sessions.Select((s, idx) => new StudyPlanSessionDto(
                d.DayIndex,
                d.Date,
                idx + 1,
                sessionDuration,
                s.RecommendationType,
                s.NodeType,
                s.NodeId,
                s.Code,
                s.Name,
                s.SubjectId,
                s.SubjectName,
                s.TopicId,
                s.TopicName,
                s.SubTopicId,
                s.SubTopicName,
                s.LearningOutcomeId,
                s.LearningOutcomeName,
                s.ClassificationState,
                s.PriorityScore,
                s.IsOrphaned
            )).ToList();

            int plannedMinutes = sessions.Count * sessionDuration;
            return new StudyPlanDayDto(
                d.DayIndex,
                d.Date,
                plannedMinutes,
                sessions.Count,
                sessions
            );
        }).ToList();

        int totalPlannedMinutes = finalDays.Sum(d => d.PlannedMinutes);
        int unallocatedMinutes = totalAvailableMinutes - totalPlannedMinutes;
        decimal utilizationRate = totalAvailableMinutes > 0
            ? Math.Round((decimal)totalPlannedMinutes / totalAvailableMinutes, 4)
            : 0m;

        return new AcademicStudyPlanDto(
            studentId,
            clampedPlanDays,
            clampedDailyMinutes,
            sessionDuration,
            totalAvailableMinutes,
            totalPlannedMinutes,
            unallocatedMinutes,
            utilizationRate,
            candidateAllocations.Count,
            unscheduledRecommendations.Count,
            AcademicRecommendationPolicy.CurrentRecommendationPolicyVersion,
            AcademicStudyPlanPolicy.CurrentStudyPlanPolicyVersion,
            finalDays,
            unscheduledRecommendations
        );
    }

    private class DayWork(int dayIndex, DateTimeOffset? date, int capacity)
    {
        public int DayIndex { get; } = dayIndex;
        public DateTimeOffset? Date { get; } = date;
        public int Capacity { get; } = capacity;
        public List<AcademicRecommendationDto> Sessions { get; } = [];
        public HashSet<Guid> NodeIds { get; } = [];
    }
}
