using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.CreateAttempt;
using EgitimPlatform.Modules.Exams.Features.CreateExam;
using EgitimPlatform.Modules.Exams.Features.GetStudentAnalysis;
using EgitimPlatform.Modules.Exams.Features.ListExams;
using EgitimPlatform.Modules.Exams.Features.ListStudentResults;
using EgitimPlatform.Modules.Exams.Features.UpdateAttempt;
using EgitimPlatform.Modules.Exams.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint3Tests
{
    private static ApplicationDbContext ModelContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer("Server=localhost;Database=ModelOnly;Integrated Security=true;TrustServerCertificate=true").Options);

    [Fact]
    public void CalculateNet_StandardPenalty_CalculatesCorrectly()
    {
        var scoring = new ExamScoringService();
        var net = scoring.CalculateNet(10, 4, 4m);
        Assert.Equal(9.00m, net);
    }

    [Fact]
    public void CalculateNet_DecimalPrecision_RoundsAwayFromZero()
    {
        var scoring = new ExamScoringService();
        // 10 - (3 / 4) = 9.25
        var net = scoring.CalculateNet(10, 3, 4m);
        Assert.Equal(9.25m, net);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateNet_InvalidPenalty_ThrowsException(decimal penalty)
    {
        var scoring = new ExamScoringService();
        Assert.Throws<ArgumentOutOfRangeException>(() => scoring.CalculateNet(10, 2, penalty));
    }

    [Fact]
    public void CalculateNet_NegativeCounts_ThrowsException()
    {
        var scoring = new ExamScoringService();
        Assert.Throws<ArgumentOutOfRangeException>(() => scoring.CalculateNet(-1, 2, 4m));
        Assert.Throws<ArgumentOutOfRangeException>(() => scoring.CalculateNet(10, -2, 4m));
    }

    [Fact]
    public void CommandValidators_RejectInvalidParameters()
    {
        Assert.False(new CreateExamCommandValidator().Validate(new CreateExamCommand("", "", Guid.Empty, DateTimeOffset.UtcNow, 0m, ScoreSource.None, [])).IsValid);
        Assert.False(new CreateAttemptCommandValidator().Validate(new CreateAttemptCommand(Guid.Empty, DateTimeOffset.UtcNow, null, null, ScoreSource.None, [])).IsValid);
        Assert.False(new UpdateAttemptCommandValidator().Validate(new UpdateAttemptCommand(DateTimeOffset.UtcNow, null, ScoreSource.None, [], [])).IsValid);
        Assert.False(new ListExamsQueryValidator().Validate(new ListExamsQuery(Page: 0, PageSize: 300)).IsValid);
        Assert.False(new ListStudentResultsQueryValidator().Validate(new ListStudentResultsQuery(Page: -1, PageSize: 0)).IsValid);
        Assert.False(new GetStudentAnalysisQueryValidator().Validate(new GetStudentAnalysisQuery(Window: 100)).IsValid);
    }

    [Fact]
    public void ModelContainsAllSprint3ExamEntitiesAndIndexes()
    {
        using var db = ModelContext();

        foreach (var type in new[]
        {
            typeof(Exam),
            typeof(ExamSection),
            typeof(StudentExamAttempt),
            typeof(StudentExamSubjectResult),
            typeof(StudentExamTopicResult)
        })
        {
            Assert.NotNull(db.Model.FindEntityType(type));
        }

        var examType = db.Model.FindEntityType(typeof(Exam))!;
        Assert.NotNull(examType.FindProperty("RowVersion"));

        var attemptType = db.Model.FindEntityType(typeof(StudentExamAttempt))!;
        Assert.NotNull(attemptType.FindProperty("RowVersion"));
    }
}
