namespace EgitimPlatform.Modules.Exams.Services;

public interface IExamScoringService
{
    decimal CalculateNet(int correct, int wrong, decimal penalty);
}

public class ExamScoringService : IExamScoringService
{
    public decimal CalculateNet(int correct, int wrong, decimal penalty)
    {
        if (correct < 0) throw new ArgumentOutOfRangeException(nameof(correct), "Correct count cannot be negative.");
        if (wrong < 0) throw new ArgumentOutOfRangeException(nameof(wrong), "Wrong count cannot be negative.");
        if (penalty <= 0m) throw new ArgumentOutOfRangeException(nameof(penalty), "Penalty must be greater than zero.");

        var rawNet = (decimal)correct - ((decimal)wrong / penalty);
        return Math.Round(rawNet, 2, MidpointRounding.AwayFromZero);
    }
}
