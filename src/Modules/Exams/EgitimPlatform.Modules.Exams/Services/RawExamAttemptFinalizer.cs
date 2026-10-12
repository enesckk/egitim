using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;

namespace EgitimPlatform.Modules.Exams.Services;

public interface IRawExamAttemptFinalizer
{
    void FinalizeRawAttempt(
        StudentExamAttempt attempt,
        Exam exam,
        IReadOnlyDictionary<Guid, ExamSection> sections,
        IReadOnlyList<ExamQuestion> examQuestions,
        List<StudentExamQuestionResult> rawAnswers,
        IReadOnlyDictionary<Guid, Guid?> questionTopics,
        IExamScoringService scoringService,
        DateTimeOffset finalizedAt);
}

public class RawExamAttemptFinalizer : IRawExamAttemptFinalizer
{
    public void FinalizeRawAttempt(
        StudentExamAttempt attempt,
        Exam exam,
        IReadOnlyDictionary<Guid, ExamSection> sections,
        IReadOnlyList<ExamQuestion> examQuestions,
        List<StudentExamQuestionResult> rawAnswers,
        IReadOnlyDictionary<Guid, Guid?> questionTopics,
        IExamScoringService scoringService,
        DateTimeOffset finalizedAt)
    {
        var rawMap = rawAnswers.ToDictionary(r => r.ExamQuestionId);

        // Ensure a StudentExamQuestionResult exists for every ExamQuestion in the exam
        foreach (var eq in examQuestions)
        {
            if (!rawMap.TryGetValue(eq.Id, out var resultRecord))
            {
                resultRecord = new StudentExamQuestionResult
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = attempt.InstitutionId,
                    StudentExamAttemptId = attempt.Id,
                    ExamQuestionId = eq.Id,
                    QuestionId = eq.QuestionId,
                    SelectedOption = null,
                    IsBlank = true,
                    IsCorrect = null,
                    NetContribution = 0m
                };
                rawMap[eq.Id] = resultRecord;
                rawAnswers.Add(resultRecord);
            }
        }

        // Score every question result against ExamQuestion.CorrectOption snapshot
        foreach (var r in rawAnswers)
        {
            var eq = examQuestions.First(x => x.Id == r.ExamQuestionId);
            var secPenalty = sections.TryGetValue(eq.ExamSectionId, out var sec) && sec.WrongAnswerPenalty.HasValue
                ? sec.WrongAnswerPenalty.Value
                : exam.WrongAnswerPenalty;

            if (r.IsBlank || string.IsNullOrWhiteSpace(r.SelectedOption))
            {
                r.SelectedOption = null;
                r.IsBlank = true;
                r.IsCorrect = null;
                r.NetContribution = 0m;
            }
            else if (string.Equals(r.SelectedOption.Trim(), eq.CorrectOption.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                r.IsCorrect = true;
                r.IsBlank = false;
                r.NetContribution = 1m;
            }
            else
            {
                r.IsCorrect = false;
                r.IsBlank = false;
                var penaltyAmount = secPenalty > 0m ? (1m / secPenalty) : 0m;
                r.NetContribution = Math.Round(-penaltyAmount, 4, MidpointRounding.AwayFromZero);
            }
        }

        // Rebuild Subject Results
        var newSubjectResults = new List<StudentExamSubjectResult>();
        var answersBySection = rawAnswers.GroupBy(r => examQuestions.First(eq => eq.Id == r.ExamQuestionId).ExamSectionId);

        foreach (var group in answersBySection)
        {
            var sectionId = group.Key;
            var secPenalty = sections.TryGetValue(sectionId, out var sec) && sec.WrongAnswerPenalty.HasValue
                ? sec.WrongAnswerPenalty.Value
                : exam.WrongAnswerPenalty;

            int correct = group.Count(r => r.IsCorrect == true);
            int wrong = group.Count(r => r.IsCorrect == false);
            int blank = group.Count(r => r.IsBlank);

            var sr = new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = attempt.InstitutionId,
                StudentExamAttemptId = attempt.Id,
                ExamSectionId = sectionId,
                Correct = correct,
                Wrong = wrong,
                Blank = blank,
                Net = scoringService.CalculateNet(correct, wrong, secPenalty)
            };
            newSubjectResults.Add(sr);
        }

        // Rebuild Topic Results
        var newTopicResults = new List<StudentExamTopicResult>();
        var answersBySectionAndTopic = rawAnswers.GroupBy(r =>
        {
            var eq = examQuestions.First(x => x.Id == r.ExamQuestionId);
            var topicId = questionTopics.GetValueOrDefault(eq.QuestionId);
            return (eq.ExamSectionId, TopicId: topicId);
        });

        foreach (var group in answersBySectionAndTopic)
        {
            var (sectionId, topicId) = group.Key;
            if (!topicId.HasValue || topicId.Value == Guid.Empty) continue;

            var secPenalty = sections.TryGetValue(sectionId, out var sec) && sec.WrongAnswerPenalty.HasValue
                ? sec.WrongAnswerPenalty.Value
                : exam.WrongAnswerPenalty;

            int correct = group.Count(r => r.IsCorrect == true);
            int wrong = group.Count(r => r.IsCorrect == false);
            int blank = group.Count(r => r.IsBlank);

            var tr = new StudentExamTopicResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = attempt.InstitutionId,
                StudentExamAttemptId = attempt.Id,
                ExamSectionId = sectionId,
                TopicId = topicId.Value,
                Correct = correct,
                Wrong = wrong,
                Blank = blank,
                Net = scoringService.CalculateNet(correct, wrong, secPenalty)
            };
            newTopicResults.Add(tr);
        }

        // Assign to Attempt
        attempt.SubjectResults = newSubjectResults;
        attempt.TopicResults = newTopicResults;
        attempt.QuestionResults = rawAnswers;

        attempt.TotalCorrect = newSubjectResults.Sum(s => s.Correct);
        attempt.TotalWrong = newSubjectResults.Sum(s => s.Wrong);
        attempt.TotalBlank = newSubjectResults.Sum(s => s.Blank);
        attempt.TotalNet = newSubjectResults.Sum(s => s.Net);

        attempt.IsRawAvailable = true;
        attempt.Status = AttemptStatus.Finalized;
        attempt.FinalizedAt = finalizedAt;
    }
}
