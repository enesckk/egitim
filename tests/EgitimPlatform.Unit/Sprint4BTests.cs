using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Questions.Features.CreateQuestion;
using FluentValidation;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint4BTests
{
    [Fact]
    public void CreateQuestionCommandValidator_Valid5OptionQuestion_PassesValidation()
    {
        var command = new CreateQuestionCommand(
            SubjectId: Guid.NewGuid(),
            TopicId: Guid.NewGuid(),
            SubTopicId: null,
            LearningOutcomeId: null,
            Difficulty: DifficultyLevel.Medium,
            SourceType: QuestionSourceType.InstitutionCustom,
            SourceYear: 2024,
            Content: "What is 2 + 2?",
            MediaUrl: null,
            Options:
            [
                new QuestionOptionDto("A", "3", false),
                new QuestionOptionDto("B", "4", true),
                new QuestionOptionDto("C", "5", false),
                new QuestionOptionDto("D", "6", false),
                new QuestionOptionDto("E", "7", false)
            ]
        );

        var validator = new CreateQuestionCommandValidator();
        var result = validator.Validate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateQuestionCommandValidator_DuplicateOptionLabels_FailsValidation()
    {
        var command = new CreateQuestionCommand(
            SubjectId: Guid.NewGuid(),
            TopicId: Guid.NewGuid(),
            SubTopicId: null,
            LearningOutcomeId: null,
            Difficulty: DifficultyLevel.Medium,
            SourceType: QuestionSourceType.InstitutionCustom,
            SourceYear: 2024,
            Content: "What is 2 + 2?",
            MediaUrl: null,
            Options:
            [
                new QuestionOptionDto("A", "3", false),
                new QuestionOptionDto("A", "4", true), // Duplicate label A
                new QuestionOptionDto("C", "5", false),
                new QuestionOptionDto("D", "6", false),
                new QuestionOptionDto("E", "7", false)
            ]
        );

        var validator = new CreateQuestionCommandValidator();
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("unique labels"));
    }

    [Fact]
    public void CreateQuestionCommandValidator_ZeroCorrectOptions_FailsValidation()
    {
        var command = new CreateQuestionCommand(
            SubjectId: Guid.NewGuid(),
            TopicId: Guid.NewGuid(),
            SubTopicId: null,
            LearningOutcomeId: null,
            Difficulty: DifficultyLevel.Medium,
            SourceType: QuestionSourceType.InstitutionCustom,
            SourceYear: 2024,
            Content: "What is 2 + 2?",
            MediaUrl: null,
            Options:
            [
                new QuestionOptionDto("A", "3", false),
                new QuestionOptionDto("B", "4", false),
                new QuestionOptionDto("C", "5", false),
                new QuestionOptionDto("D", "6", false),
                new QuestionOptionDto("E", "7", false)
            ]
        );

        var validator = new CreateQuestionCommandValidator();
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Exactly ONE option must be marked as correct"));
    }

    [Fact]
    public void CreateQuestionCommandValidator_MultipleCorrectOptions_FailsValidation()
    {
        var command = new CreateQuestionCommand(
            SubjectId: Guid.NewGuid(),
            TopicId: Guid.NewGuid(),
            SubTopicId: null,
            LearningOutcomeId: null,
            Difficulty: DifficultyLevel.Medium,
            SourceType: QuestionSourceType.InstitutionCustom,
            SourceYear: 2024,
            Content: "What is 2 + 2?",
            MediaUrl: null,
            Options:
            [
                new QuestionOptionDto("A", "3", true),
                new QuestionOptionDto("B", "4", true), // Two correct options
                new QuestionOptionDto("C", "5", false),
                new QuestionOptionDto("D", "6", false),
                new QuestionOptionDto("E", "7", false)
            ]
        );

        var validator = new CreateQuestionCommandValidator();
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Exactly ONE option must be marked as correct"));
    }

    [Fact]
    public void CreateQuestionCommandValidator_InvalidOptionLabel_FailsValidation()
    {
        var command = new CreateQuestionCommand(
            SubjectId: Guid.NewGuid(),
            TopicId: Guid.NewGuid(),
            SubTopicId: null,
            LearningOutcomeId: null,
            Difficulty: DifficultyLevel.Medium,
            SourceType: QuestionSourceType.InstitutionCustom,
            SourceYear: 2024,
            Content: "What is 2 + 2?",
            MediaUrl: null,
            Options:
            [
                new QuestionOptionDto("A", "3", false),
                new QuestionOptionDto("B", "4", true),
                new QuestionOptionDto("C", "5", false),
                new QuestionOptionDto("D", "6", false),
                new QuestionOptionDto("F", "7", false) // Invalid label F
            ]
        );

        var validator = new CreateQuestionCommandValidator();
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
    }
}
