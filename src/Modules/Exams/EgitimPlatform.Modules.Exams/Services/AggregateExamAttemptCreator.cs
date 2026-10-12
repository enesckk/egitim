using EgitimPlatform.Modules.Exams.Entities;

namespace EgitimPlatform.Modules.Exams.Services;

public interface IAggregateExamAttemptCreator
{
    StudentExamAttempt CreateFinalizedAggregateAttempt(
        Guid institutionId,
        Guid examId,
        Guid studentId,
        int attemptNumber,
        IReadOnlyDictionary<Guid, ExamSection> sectionsDict,
        Exam exam,
        IEnumerable<(Guid ExamSectionId, int Correct, int Wrong, int Blank)> sectionCounts,
        DateTimeOffset finalizedAt,
        DateTimeOffset? takenAt = null);
}

public class AggregateExamAttemptCreator(IExamScoringService scoringService) : IAggregateExamAttemptCreator
{
    public StudentExamAttempt CreateFinalizedAggregateAttempt(
        Guid institutionId,
        Guid examId,
        Guid studentId,
        int attemptNumber,
        IReadOnlyDictionary<Guid, ExamSection> sectionsDict,
        Exam exam,
        IEnumerable<(Guid ExamSectionId, int Correct, int Wrong, int Blank)> sectionCounts,
        DateTimeOffset finalizedAt,
        DateTimeOffset? takenAt = null)
    {
        var attemptId = Guid.NewGuid();
        var subjectResults = new List<StudentExamSubjectResult>();

        foreach (var secInput in sectionCounts)
        {
            var secPenalty = sectionsDict.TryGetValue(secInput.ExamSectionId, out var sec) && sec.WrongAnswerPenalty.HasValue
                ? sec.WrongAnswerPenalty.Value
                : exam.WrongAnswerPenalty;

            var net = scoringService.CalculateNet(secInput.Correct, secInput.Wrong, secPenalty);

            subjectResults.Add(new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = institutionId,
                StudentExamAttemptId = attemptId,
                ExamSectionId = secInput.ExamSectionId,
                Correct = secInput.Correct,
                Wrong = secInput.Wrong,
                Blank = secInput.Blank,
                Net = net
            });
        }

        int totalCorrect = subjectResults.Sum(s => s.Correct);
        int totalWrong = subjectResults.Sum(s => s.Wrong);
        int totalBlank = subjectResults.Sum(s => s.Blank);
        decimal totalNet = subjectResults.Sum(s => s.Net);

        return new StudentExamAttempt
        {
            Id = attemptId,
            InstitutionId = institutionId,
            ExamId = examId,
            StudentId = studentId,
            AttemptNumber = attemptNumber,
            TakenAt = takenAt ?? exam.ExamDate,
            Status = AttemptStatus.Finalized,
            FinalizedAt = finalizedAt,
            IsRawAvailable = false,
            TotalCorrect = totalCorrect,
            TotalWrong = totalWrong,
            TotalBlank = totalBlank,
            TotalNet = totalNet,
            SubjectResults = subjectResults,
            TopicResults = [],
            QuestionResults = []
        };
    }
}
