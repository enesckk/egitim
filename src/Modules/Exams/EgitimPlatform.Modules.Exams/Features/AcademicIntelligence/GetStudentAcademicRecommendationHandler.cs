using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;

namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public class GetStudentAcademicRecommendationHandler(
    IExamResourceAccess resourceAccess,
    GetStudentKnowledgeMapHandler knowledgeMapHandler)
{
    private static void ValidateQueryParameters(AcademicRecommendationQuery query)
    {
        if (query.RecommendationLimit < AcademicRecommendationPolicy.MinRecommendationLimit ||
            query.RecommendationLimit > AcademicRecommendationPolicy.MaxRecommendationLimit)
        {
            throw new ValidationException($"RecommendationLimit must be between {AcademicRecommendationPolicy.MinRecommendationLimit} and {AcademicRecommendationPolicy.MaxRecommendationLimit}.");
        }

        if (query.WindowSize < 2 || query.WindowSize > 10)
            throw new ValidationException("WindowSize must be between 2 and 10.");

        if (query.Limit.HasValue && (query.Limit.Value < 1 || query.Limit.Value > 100))
            throw new ValidationException("Limit must be between 1 and 100.");

        if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value)
            throw new ValidationException("From date cannot be after To date.");
    }

    public async Task<List<AcademicRecommendationDto>> GetRecommendationsAsync(
        AcademicRecommendationQuery query,
        CancellationToken ct = default)
    {
        ValidateQueryParameters(query);
        await resourceAccess.ValidateStudentReadAccessAsync(query.StudentId, ct);

        var classificationQuery = new AcademicClassificationQuery(
            query.StudentId,
            query.ExamTypeId,
            query.WindowSize,
            query.Limit,
            query.From,
            query.To
        );

        var knowledgeMap = await knowledgeMapHandler.GetKnowledgeMapAsync(classificationQuery, ct);

        List<AcademicRecommendationDto> loCandidates = [];
        List<AcademicRecommendationDto> topicCandidates = [];
        List<AcademicRecommendationDto> subjectCandidates = [];

        foreach (var subject in knowledgeMap.Subjects)
        {
            foreach (var topic in subject.Topics)
            {
                foreach (var subTopic in topic.SubTopics)
                {
                    foreach (var lo in subTopic.LearningOutcomes)
                    {
                        var loRecType = DetermineRecommendationType(lo.State, lo.TrendDirection, lo.HasEvidence);
                        if (loRecType.HasValue)
                        {
                            int score = AcademicRecommendationCalculator.CalculatePriorityScore(
                                lo.State,
                                lo.TrendDirection,
                                lo.SuccessRate,
                                RecommendationNodeType.LearningOutcome
                            );

                            loCandidates.Add(new AcademicRecommendationDto(
                                loRecType.Value,
                                RecommendationNodeType.LearningOutcome,
                                lo.Id,
                                lo.Code,
                                lo.Name,
                                subject.Id,
                                subject.Name,
                                topic.Id,
                                topic.Name,
                                subTopic.Id,
                                subTopic.Name,
                                lo.Id,
                                lo.Name,
                                lo.State,
                                lo.Basis,
                                lo.SuccessRate,
                                lo.Accuracy,
                                lo.Coverage,
                                lo.TrendDirection,
                                lo.SuccessRateDelta,
                                lo.ObservationCount,
                                lo.TotalQuestions,
                                score,
                                AcademicClassificationPolicy.CurrentPolicyVersion,
                                AcademicRecommendationPolicy.CurrentRecommendationPolicyVersion,
                                lo.IsOrphaned
                            ));
                        }
                    }

                    // SubTopics are structural only in Sprint 6E
                }

                var topicRecType = DetermineRecommendationType(topic.State, topic.TrendDirection, topic.HasEvidence);
                if (topicRecType.HasValue)
                {
                    int score = AcademicRecommendationCalculator.CalculatePriorityScore(
                        topic.State,
                        topic.TrendDirection,
                        topic.SuccessRate,
                        RecommendationNodeType.Topic
                    );

                    topicCandidates.Add(new AcademicRecommendationDto(
                        topicRecType.Value,
                        RecommendationNodeType.Topic,
                        topic.Id,
                        topic.Code,
                        topic.Name,
                        subject.Id,
                        subject.Name,
                        topic.Id,
                        topic.Name,
                        null,
                        null,
                        null,
                        null,
                        topic.State,
                        topic.Basis,
                        topic.SuccessRate,
                        topic.Accuracy,
                        topic.Coverage,
                        topic.TrendDirection,
                        topic.SuccessRateDelta,
                        topic.ObservationCount,
                        topic.TotalQuestions,
                        score,
                        AcademicClassificationPolicy.CurrentPolicyVersion,
                        AcademicRecommendationPolicy.CurrentRecommendationPolicyVersion,
                        topic.IsOrphaned
                    ));
                }
            }

            var subjectRecType = DetermineRecommendationType(subject.State, subject.TrendDirection, subject.HasEvidence);
            if (subjectRecType.HasValue)
            {
                int score = AcademicRecommendationCalculator.CalculatePriorityScore(
                    subject.State,
                    subject.TrendDirection,
                    subject.SuccessRate,
                    RecommendationNodeType.Subject
                );

                subjectCandidates.Add(new AcademicRecommendationDto(
                    subjectRecType.Value,
                    RecommendationNodeType.Subject,
                    subject.Id,
                    subject.Code,
                    subject.Name,
                    subject.Id,
                    subject.Name,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    subject.State,
                    subject.Basis,
                    subject.SuccessRate,
                    subject.Accuracy,
                    subject.Coverage,
                    subject.TrendDirection,
                    subject.SuccessRateDelta,
                    subject.ObservationCount,
                    subject.TotalQuestions,
                    score,
                    AcademicClassificationPolicy.CurrentPolicyVersion,
                    AcademicRecommendationPolicy.CurrentRecommendationPolicyVersion,
                    subject.IsOrphaned
                ));
            }
        }

        // Ancestral / Descendant Specificity Suppression:
        // If an LO candidate is selected for remediation (Critical or Weak), suppress its direct parent Topic candidate
        // from creating a redundant recommendation for the same underlying weakness path.
        var loParentTopicIds = loCandidates
            .Where(l => l.RecommendationType is AcademicRecommendationType.RemediateCritical or AcademicRecommendationType.StrengthenWeak)
            .Select(l => l.TopicId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var filteredTopicCandidates = topicCandidates
            .Where(t => !loParentTopicIds.Contains(t.NodeId))
            .ToList();

        var topicParentSubjectIds = filteredTopicCandidates.Concat(topicCandidates)
            .Where(t => t.RecommendationType is AcademicRecommendationType.RemediateCritical or AcademicRecommendationType.StrengthenWeak)
            .Select(t => t.SubjectId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var filteredSubjectCandidates = subjectCandidates
            .Where(s => !topicParentSubjectIds.Contains(s.NodeId))
            .ToList();

        var allCandidates = loCandidates
            .Concat(filteredTopicCandidates)
            .Concat(filteredSubjectCandidates);

        return AcademicRecommendationCalculator.SortAndLimitRecommendations(allCandidates, query.RecommendationLimit);
    }

    private static AcademicRecommendationType? DetermineRecommendationType(
        AcademicClassificationState state,
        TrendDirection? trend,
        bool hasEvidence)
    {
        if (!hasEvidence || state == AcademicClassificationState.InsufficientData)
            return null;

        return state switch
        {
            AcademicClassificationState.Critical => AcademicRecommendationType.RemediateCritical,
            AcademicClassificationState.Weak => AcademicRecommendationType.StrengthenWeak,
            AcademicClassificationState.Developing => trend == TrendDirection.Decreasing
                ? AcademicRecommendationType.ReinforceDeveloping
                : AcademicRecommendationType.ReinforceDeveloping,
            AcademicClassificationState.Strong => null,
            _ => null
        };
    }
}
