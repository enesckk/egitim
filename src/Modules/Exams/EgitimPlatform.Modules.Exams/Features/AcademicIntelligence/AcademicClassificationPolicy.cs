namespace EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;

public static class AcademicClassificationPolicy
{
    public const string CurrentPolicyVersion = "v1";

    // Performance Thresholds (SuccessRate)
    public const decimal CriticalThresholdUpperExclusive = 0.40m;   // < 0.40 -> Critical
    public const decimal WeakThresholdUpperExclusive = 0.60m;       // 0.40 <= SR < 0.60 -> Weak
    public const decimal DevelopingThresholdUpperExclusive = 0.80m; // 0.60 <= SR < 0.80 -> Developing
    // >= 0.80 -> Strong

    // Sufficiency Bounds per Taxonomy Level
    // Subject
    public const int SubjectMinObservations = 2;
    public const int SubjectMinQuestions = 10;

    // Topic
    public const int TopicMinObservations = 2;
    public const int TopicMinQuestions = 5;

    // LearningOutcome
    public const int LearningOutcomeMinObservations = 2;
    public const int LearningOutcomeMinQuestions = 3;

    // QuestionStyle (Sprint B)
    public const int QuestionStyleMinObservations = 2;
    public const int QuestionStyleMinQuestions = 5;
}
