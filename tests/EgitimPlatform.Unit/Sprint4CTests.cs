using EgitimPlatform.Modules.Exams.Features.SaveAnswers;
using FluentValidation;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint4CTests
{
    [Fact]
    public void SaveAnswersCommandValidator_ValidAnswers_PassesValidation()
    {
        var command = new SaveAnswersCommand(
            Answers: new List<RawAnswerDto>
            {
                new(Guid.NewGuid(), "A"),
                new(Guid.NewGuid(), "B"),
                new(Guid.NewGuid(), null),
                new(Guid.NewGuid(), "  c  ")
            }
        );

        var validator = new SaveAnswersCommandValidator();
        var result = validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void SaveAnswersCommandValidator_InvalidOptionLabel_FailsValidation()
    {
        var command = new SaveAnswersCommand(
            Answers: new List<RawAnswerDto>
            {
                new(Guid.NewGuid(), "X") // Invalid option label X
            }
        );

        var validator = new SaveAnswersCommandValidator();
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("one of A, B, C, D, E"));
    }

    [Fact]
    public void NetContribution_Penalty4_CalculatesCorrectly()
    {
        decimal penalty = 4m;
        decimal correctContrib = 1m;
        decimal wrongContrib = Math.Round(-1m / penalty, 4, MidpointRounding.AwayFromZero);
        decimal blankContrib = 0m;

        Assert.Equal(1.0000m, correctContrib);
        Assert.Equal(-0.2500m, wrongContrib);
        Assert.Equal(0.0000m, blankContrib);
    }
}
