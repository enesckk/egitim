namespace EgitimPlatform.Modules.Exams.Services;

public interface IExamScoringService
{
    decimal CalculateNet(int correct, int wrong, decimal penalty = ExamScoringService.DefaultWrongAnswerPenalty);
}
