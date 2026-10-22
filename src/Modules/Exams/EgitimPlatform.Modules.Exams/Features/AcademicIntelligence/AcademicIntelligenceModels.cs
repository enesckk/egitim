namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public record StudentSubjectPerformanceDto(
    Guid SubjectId,
    string SubjectName,
    int ExamCount,
    int AttemptCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal TotalNet,
    decimal Accuracy,      // Correct / Attempted (Attempted = Correct + Wrong)
    decimal SuccessRate,   // Correct / TotalQuestions
    decimal Coverage,      // Attempted / TotalQuestions
    int RawAttemptCount,
    int AggregateAttemptCount
);

public record StudentTopicPerformanceDto(
    Guid TopicId,
    string TopicName,
    Guid SubjectId,
    string SubjectName,
    int ExamCount,
    int AttemptCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal TotalNet,
    decimal Accuracy,
    decimal SuccessRate,
    decimal Coverage
);

public record StudentLearningOutcomePerformanceDto(
    Guid LearningOutcomeId,
    string LearningOutcomeCode,
    string LearningOutcomeTitle,
    Guid? SubTopicId,
    string? SubTopicName,
    Guid TopicId,
    string TopicName,
    Guid SubjectId,
    string SubjectName,
    int ExamCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal Accuracy,
    decimal SuccessRate,
    decimal Coverage
);

public record AcademicCoverageSummaryDto(
    Guid StudentId,
    int TotalFinalizedAttempts,
    int RawAttemptsCount,
    int AggregateAttemptsCount,
    decimal RawCoveragePercentage,
    int TotalQuestionsEvaluated,
    int TaggedQuestionsEvaluated,
    decimal LearningOutcomeTagCoveragePercentage
);

public record AcademicIntelligenceQuery(
    Guid StudentId,
    Guid? ExamTypeId = null,
    int? Window = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
);

public enum TrendDirection
{
    Increasing,
    Decreasing,
    Stable
}

public record SubjectObservationPointDto(
    Guid AttemptId,
    Guid ExamId,
    string ExamTitle,
    DateTimeOffset TakenAt,
    int AttemptNumber,
    Guid SubjectId,
    int TotalQuestions,
    int Correct,
    int Wrong,
    int Blank,
    decimal Net,
    decimal NetPerQuestion,
    decimal Accuracy,
    decimal SuccessRate,
    decimal Coverage
);

public record TopicObservationPointDto(
    Guid AttemptId,
    Guid ExamId,
    string ExamTitle,
    DateTimeOffset TakenAt,
    int AttemptNumber,
    Guid TopicId,
    int TotalQuestions,
    int Correct,
    int Wrong,
    int Blank,
    decimal Net,
    decimal NetPerQuestion,
    decimal Accuracy,
    decimal SuccessRate,
    decimal Coverage
);

public record LearningOutcomeObservationPointDto(
    Guid AttemptId,
    Guid ExamId,
    string ExamTitle,
    DateTimeOffset TakenAt,
    int AttemptNumber,
    Guid LearningOutcomeId,
    int TotalQuestions,
    int Correct,
    int Wrong,
    int Blank,
    decimal Accuracy,
    decimal SuccessRate,
    decimal Coverage
);

public record TrendWindowMetricsDto(
    int ObservationCount,
    int TotalQuestions,
    int Correct,
    int Wrong,
    int Blank,
    decimal TotalNet,
    decimal NetPerQuestion,
    decimal Accuracy,
    decimal SuccessRate,
    decimal Coverage
);

public record SubjectTrendDto(
    Guid SubjectId,
    string SubjectName,
    int TotalObservations,
    int WindowSize,
    bool HasEnoughData,
    TrendWindowMetricsDto? PreviousWindow,
    TrendWindowMetricsDto? RecentWindow,
    decimal? SuccessRateDelta,
    decimal? AccuracyDelta,
    decimal? CoverageDelta,
    decimal? NetPerQuestionDelta,
    TrendDirection? Direction,
    List<SubjectObservationPointDto> TimeSeries
);

public record TopicTrendDto(
    Guid TopicId,
    string TopicName,
    Guid SubjectId,
    string SubjectName,
    int TotalObservations,
    int WindowSize,
    bool HasEnoughData,
    TrendWindowMetricsDto? PreviousWindow,
    TrendWindowMetricsDto? RecentWindow,
    decimal? SuccessRateDelta,
    decimal? AccuracyDelta,
    decimal? CoverageDelta,
    decimal? NetPerQuestionDelta,
    TrendDirection? Direction,
    List<TopicObservationPointDto> TimeSeries
);

public record LearningOutcomeTrendDto(
    Guid LearningOutcomeId,
    string LearningOutcomeCode,
    string LearningOutcomeTitle,
    Guid TopicId,
    string TopicName,
    Guid SubjectId,
    string SubjectName,
    int TotalObservations,
    int WindowSize,
    bool HasEnoughData,
    TrendWindowMetricsDto? PreviousWindow,
    TrendWindowMetricsDto? RecentWindow,
    decimal? SuccessRateDelta,
    decimal? AccuracyDelta,
    decimal? CoverageDelta,
    TrendDirection? Direction,
    List<LearningOutcomeObservationPointDto> TimeSeries
);

public record AcademicTrendQuery(
    Guid StudentId,
    Guid? ExamTypeId = null,
    int WindowSize = 3,
    int? Limit = 50,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
);

public enum AcademicClassificationState
{
    InsufficientData,
    Critical,
    Weak,
    Developing,
    Strong
}

public enum ClassificationInsufficiencyReason
{
    None,
    NoData,
    TooFewObservations,
    TooFewQuestions,
    NoRawData
}

public enum ClassificationBasis
{
    RecentWindow,
    OverallHistory
}

public record AcademicClassificationQuery(
    Guid StudentId,
    Guid? ExamTypeId = null,
    int WindowSize = 3,
    int? Limit = 50,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
);

public record SubjectClassificationDto(
    Guid SubjectId,
    string SubjectName,
    AcademicClassificationState State,
    bool HasEnoughData,
    ClassificationBasis Basis,
    int ObservationCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal SuccessRate,
    decimal Accuracy,
    decimal Coverage,
    TrendDirection? TrendDirection,
    decimal? SuccessRateDelta,
    string PolicyVersion,
    ClassificationInsufficiencyReason InsufficiencyReason
);

public record TopicClassificationDto(
    Guid TopicId,
    string TopicName,
    Guid SubjectId,
    string SubjectName,
    AcademicClassificationState State,
    bool HasEnoughData,
    ClassificationBasis Basis,
    int ObservationCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal SuccessRate,
    decimal Accuracy,
    decimal Coverage,
    TrendDirection? TrendDirection,
    decimal? SuccessRateDelta,
    string PolicyVersion,
    ClassificationInsufficiencyReason InsufficiencyReason
);

public record LearningOutcomeClassificationDto(
    Guid LearningOutcomeId,
    string LearningOutcomeCode,
    string LearningOutcomeTitle,
    Guid TopicId,
    string TopicName,
    Guid SubjectId,
    string SubjectName,
    AcademicClassificationState State,
    bool HasEnoughData,
    ClassificationBasis Basis,
    int ObservationCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal SuccessRate,
    decimal Accuracy,
    decimal Coverage,
    TrendDirection? TrendDirection,
    decimal? SuccessRateDelta,
    string PolicyVersion,
    ClassificationInsufficiencyReason InsufficiencyReason
);

public record KnowledgeMapLearningOutcomeNodeDto(
    Guid Id,
    string Code,
    string Name,
    Guid? SubTopicId,
    Guid? TopicId,
    Guid? SubjectId,
    AcademicClassificationState State,
    bool HasEnoughData,
    bool HasEvidence,
    ClassificationBasis Basis,
    int ObservationCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal? SuccessRate,
    decimal? Accuracy,
    decimal? Coverage,
    TrendDirection? TrendDirection,
    decimal? SuccessRateDelta,
    bool IsOrphaned = false
);

public record KnowledgeMapSubTopicNodeDto(
    Guid Id,
    string Code,
    string Name,
    Guid? TopicId,
    AcademicClassificationState State,
    bool HasEnoughData,
    bool HasEvidence,
    ClassificationBasis Basis,
    int ObservationCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal? SuccessRate,
    decimal? Accuracy,
    decimal? Coverage,
    TrendDirection? TrendDirection,
    decimal? SuccessRateDelta,
    List<KnowledgeMapLearningOutcomeNodeDto> LearningOutcomes,
    bool IsOrphaned = false
);

public record KnowledgeMapTopicNodeDto(
    Guid Id,
    string Code,
    string Name,
    Guid? SubjectId,
    AcademicClassificationState State,
    bool HasEnoughData,
    bool HasEvidence,
    ClassificationBasis Basis,
    int ObservationCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal? SuccessRate,
    decimal? Accuracy,
    decimal? Coverage,
    TrendDirection? TrendDirection,
    decimal? SuccessRateDelta,
    List<KnowledgeMapSubTopicNodeDto> SubTopics,
    bool IsOrphaned = false
);

public record KnowledgeMapSubjectNodeDto(
    Guid Id,
    string Code,
    string Name,
    AcademicClassificationState State,
    bool HasEnoughData,
    bool HasEvidence,
    ClassificationBasis Basis,
    int ObservationCount,
    int TotalQuestions,
    int AttemptedCount,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal? SuccessRate,
    decimal? Accuracy,
    decimal? Coverage,
    TrendDirection? TrendDirection,
    decimal? SuccessRateDelta,
    List<KnowledgeMapTopicNodeDto> Topics,
    bool IsOrphaned = false
);

public record KnowledgeMapSummaryDto(
    int TotalSubjects,
    int SubjectsWithEvidence,
    int StrongSubjects,
    int WeakSubjects,
    int CriticalSubjects,
    int TotalTopics,
    int TopicsWithEvidence,
    int TotalLearningOutcomes,
    int LearningOutcomesWithEvidence
);

public record StudentKnowledgeMapDto(
    Guid StudentId,
    string PolicyVersion,
    Guid? ExamTypeId,
    KnowledgeMapSummaryDto Summary,
    List<KnowledgeMapSubjectNodeDto> Subjects
);

public enum AcademicRecommendationType
{
    RemediateCritical,
    StrengthenWeak,
    ReinforceDeveloping,
    MonitorDeclining
}

public enum RecommendationNodeType
{
    Subject,
    Topic,
    LearningOutcome,
    QuestionStyle
}

public record AcademicRecommendationQuery(
    Guid StudentId,
    Guid? ExamTypeId = null,
    int WindowSize = 3,
    int? Limit = 50,
    int RecommendationLimit = 10,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
);

public record AcademicRecommendationDto(
    AcademicRecommendationType RecommendationType,
    RecommendationNodeType NodeType,
    Guid NodeId,
    string Code,
    string Name,
    Guid? SubjectId,
    string? SubjectName,
    Guid? TopicId,
    string? TopicName,
    Guid? SubTopicId,
    string? SubTopicName,
    Guid? LearningOutcomeId,
    string? LearningOutcomeName,
    AcademicClassificationState ClassificationState,
    ClassificationBasis ClassificationBasis,
    decimal? SuccessRate,
    decimal? Accuracy,
    decimal? Coverage,
    TrendDirection? TrendDirection,
    decimal? SuccessRateDelta,
    int ObservationCount,
    int TotalQuestions,
    int PriorityScore,
    string ClassificationPolicyVersion,
    string RecommendationPolicyVersion,
    bool IsOrphaned = false
);

public record StudyPlanSessionDto(
    int DayIndex,
    DateTimeOffset? Date,
    int Sequence,
    int DurationMinutes,
    AcademicRecommendationType RecommendationType,
    RecommendationNodeType NodeType,
    Guid NodeId,
    string Code,
    string Name,
    Guid? SubjectId,
    string? SubjectName,
    Guid? TopicId,
    string? TopicName,
    Guid? SubTopicId,
    string? SubTopicName,
    Guid? LearningOutcomeId,
    string? LearningOutcomeName,
    AcademicClassificationState ClassificationState,
    int PriorityScore,
    bool IsOrphaned = false
);

public record StudyPlanDayDto(
    int DayIndex,
    DateTimeOffset? Date,
    int PlannedMinutes,
    int SessionCount,
    List<StudyPlanSessionDto> Sessions
);

public record AcademicStudyPlanDto(
    Guid StudentId,
    int PlanDays,
    int DailyStudyMinutes,
    int SessionMinutes,
    int TotalAvailableMinutes,
    int TotalPlannedMinutes,
    int UnallocatedMinutes,
    decimal UtilizationRate,
    int ScheduledRecommendationCount,
    int UnscheduledRecommendationCount,
    string RecommendationPolicyVersion,
    string StudyPlanPolicyVersion,
    List<StudyPlanDayDto> Days,
    List<AcademicRecommendationDto> UnscheduledRecommendations
);

public record AcademicStudyPlanQuery(
    Guid StudentId,
    Guid? ExamTypeId = null,
    int WindowSize = 3,
    int? Limit = 50,
    int RecommendationLimit = 10,
    int PlanDays = 7,
    int DailyStudyMinutes = 120,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
);

// --- SPRINT B: QUESTION STYLE ANALYTICS & TRENDS ---

public record StudentQuestionStyleAnalyticsDto(
    Guid QuestionStyleId,
    string Code,
    string Name,
    int TotalQuestions,
    int AttemptedQuestions,
    int CorrectCount,
    int IncorrectCount,
    int BlankCount,
    decimal? Accuracy,
    decimal? SuccessRate,
    decimal Coverage,
    int ObservationCount,
    AcademicClassificationState ClassificationState,
    TrendDirection? TrendDirection = null
);

public record QuestionStyleObservationPointDto(
    Guid AttemptId,
    Guid ExamId,
    string ExamTitle,
    DateTimeOffset TakenAt,
    int AttemptNumber,
    Guid QuestionStyleId,
    int TotalQuestions,
    int Correct,
    int Wrong,
    int Blank,
    decimal Net,
    decimal? Accuracy,
    decimal? SuccessRate,
    decimal Coverage
);

public record QuestionStyleTrendDto(
    Guid QuestionStyleId,
    string Code,
    string Name,
    int TotalObservations,
    int WindowSize,
    bool HasEnoughData,
    TrendWindowMetricsDto? PreviousWindow,
    TrendWindowMetricsDto? RecentWindow,
    decimal? SuccessRateDelta,
    decimal? AccuracyDelta,
    decimal? CoverageDelta,
    TrendDirection? Direction,
    List<QuestionStyleObservationPointDto> TimeSeries
);




