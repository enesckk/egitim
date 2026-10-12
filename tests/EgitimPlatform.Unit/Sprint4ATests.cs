using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint4ATests
{
    private static ApplicationDbContext ModelContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=true;TrustServerCertificate=true").Options);

    [Fact]
    public void StudentExamAttempt_DefaultIsRawAvailable_IsFalse()
    {
        var attempt = new StudentExamAttempt();
        Assert.False(attempt.IsRawAvailable);
        Assert.Empty(attempt.QuestionResults);
    }

    [Fact]
    public void Question_Defaults_AreCorrect()
    {
        var question = new Question();
        Assert.Null(question.InstitutionId);
        Assert.Equal(QuestionType.MultipleChoice, question.QuestionType);
        Assert.Equal(DifficultyLevel.Medium, question.Difficulty);
        Assert.Equal(QuestionSourceType.InstitutionCustom, question.SourceType);
        Assert.Equal(QuestionStatus.Draft, question.Status);
        Assert.Empty(question.Options);
    }

    [Fact]
    public void QuestionOption_Defaults_AreCorrect()
    {
        var option = new QuestionOption
        {
            OptionLabel = "A",
            Content = "Test content",
            IsCorrect = true
        };

        Assert.Equal("A", option.OptionLabel);
        Assert.Equal("Test content", option.Content);
        Assert.True(option.IsCorrect);
    }

    [Fact]
    public void ExamQuestion_Defaults_AreCorrect()
    {
        var examQuestion = new ExamQuestion
        {
            QuestionNumber = 1,
            Order = 1,
            CorrectOption = "B"
        };

        Assert.Equal(1, examQuestion.QuestionNumber);
        Assert.Equal(1, examQuestion.Order);
        Assert.Equal("B", examQuestion.CorrectOption);
    }

    [Fact]
    public void StudentExamQuestionResult_Defaults_AreCorrect()
    {
        var result = new StudentExamQuestionResult();
        Assert.True(result.IsBlank);
        Assert.Null(result.SelectedOption);
        Assert.Null(result.IsCorrect);
        Assert.Equal(0m, result.NetContribution);
    }

    [Fact]
    public void ModelContainsAllSprint4AEntitiesAndConfigurations()
    {
        using var db = ModelContext();

        foreach (var type in new[]
        {
            typeof(Question),
            typeof(QuestionOption),
            typeof(ExamQuestion),
            typeof(StudentExamQuestionResult)
        })
        {
            Assert.NotNull(db.Model.FindEntityType(type));
        }

        var questionType = db.Model.FindEntityType(typeof(Question))!;
        Assert.NotNull(questionType.FindProperty("RowVersion"));

        var attemptType = db.Model.FindEntityType(typeof(StudentExamAttempt))!;
        Assert.NotNull(attemptType.FindProperty("IsRawAvailable"));
    }

    [Fact]
    public void ModelContainsSprint4AUniqueConstraints()
    {
        using var db = ModelContext();

        var examQuestionEntityType = db.Model.FindEntityType(typeof(ExamQuestion))!;
        var indexes = examQuestionEntityType.GetIndexes();
        Assert.Contains(indexes, i => i.IsUnique && i.Properties.Any(p => p.Name == "QuestionNumber"));
        Assert.Contains(indexes, i => i.IsUnique && i.Properties.Any(p => p.Name == "QuestionId"));

        var questionResultEntityType = db.Model.FindEntityType(typeof(StudentExamQuestionResult))!;
        var resultIndexes = questionResultEntityType.GetIndexes();
        Assert.Contains(resultIndexes, i => i.IsUnique && i.Properties.Any(p => p.Name == "ExamQuestionId"));
    }
}
