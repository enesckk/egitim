using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.CreateManualResult;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint5DUnitTests
{
    [Fact]
    public void AggregateExamAttemptCreator_CalculatesSubjectResultsAndTotalsCorrectly()
    {
        var scoringService = new ExamScoringService();
        var creator = new AggregateExamAttemptCreator(scoringService);

        var instId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var sec1Id = Guid.NewGuid();
        var sec2Id = Guid.NewGuid();

        var exam = new Exam
        {
            Id = examId,
            InstitutionId = instId,
            WrongAnswerPenalty = 4.0m
        };

        var sec1 = new ExamSection { Id = sec1Id, ExamId = examId, WrongAnswerPenalty = 4.0m, QuestionCount = 10 };
        var sec2 = new ExamSection { Id = sec2Id, ExamId = examId, WrongAnswerPenalty = 3.0m, QuestionCount = 10 };

        var sectionsDict = new Dictionary<Guid, ExamSection>
        {
            [sec1Id] = sec1,
            [sec2Id] = sec2
        };

        var sectionCounts = new List<(Guid ExamSectionId, int Correct, int Wrong, int Blank)>
        {
            (sec1Id, 8, 2, 0), // Net = 8 - 2/4 = 7.50
            (sec2Id, 7, 3, 0)  // Net = 7 - 3/3 = 6.00
        };

        var now = DateTimeOffset.UtcNow;
        var attempt = creator.CreateFinalizedAggregateAttempt(instId, examId, studentId, 1, sectionsDict, exam, sectionCounts, now);

        Assert.NotEqual(Guid.Empty, attempt.Id);
        Assert.Equal(instId, attempt.InstitutionId);
        Assert.Equal(examId, attempt.ExamId);
        Assert.Equal(studentId, attempt.StudentId);
        Assert.Equal(1, attempt.AttemptNumber);
        Assert.Equal(AttemptStatus.Finalized, attempt.Status);
        Assert.Equal(now, attempt.FinalizedAt);
        Assert.False(attempt.IsRawAvailable);

        Assert.Equal(15, attempt.TotalCorrect);
        Assert.Equal(5, attempt.TotalWrong);
        Assert.Equal(0, attempt.TotalBlank);
        Assert.Equal(13.50m, attempt.TotalNet);

        Assert.Equal(2, attempt.SubjectResults.Count);
        Assert.Empty(attempt.TopicResults);
        Assert.Empty(attempt.QuestionResults);

        var sr1 = attempt.SubjectResults.First(s => s.ExamSectionId == sec1Id);
        Assert.Equal(8, sr1.Correct);
        Assert.Equal(2, sr1.Wrong);
        Assert.Equal(7.50m, sr1.Net);

        var sr2 = attempt.SubjectResults.First(s => s.ExamSectionId == sec2Id);
        Assert.Equal(7, sr2.Correct);
        Assert.Equal(3, sr2.Wrong);
        Assert.Equal(6.00m, sr2.Net);
    }

    [Fact]
    public async Task CreateSingleManualResult_NegativeCount_ThrowsValidationException()
    {
        var db = Substitute.For<IApplicationDbContext>();
        var currentUser = Substitute.For<ICurrentUser>();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var studentDirectory = Substitute.For<IStudentDirectory>();
        var aggregateCreator = Substitute.For<IAggregateExamAttemptCreator>();
        var auditService = Substitute.For<IAuditService>();

        var examId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var secId = Guid.NewGuid();
        var instId = Guid.NewGuid();

        var exam = new Exam { Id = examId, InstitutionId = instId };
        resourceAccess.ValidateExamAccessAsync(examId, Arg.Any<CancellationToken>()).Returns(exam);
        studentDirectory.GetStudentAsync(studentId, Arg.Any<CancellationToken>())
            .Returns(new StudentReferenceDto(studentId, instId, null));

        var handler = new CreateSingleManualResultHandler(db, currentUser, resourceAccess, studentDirectory, aggregateCreator, auditService);

        var command = new CreateSingleManualResultCommand(studentId, [
            new ManualSectionResultInputDto(secId, -1, 0, 5)
        ]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(examId, command));
        Assert.Contains("INVALID_CORRECT_COUNT", ex.Message);
    }

    [Fact]
    public async Task CreateBulkManualResult_ExceedsLimit_ThrowsValidationException()
    {
        var db = Substitute.For<IApplicationDbContext>();
        var currentUser = Substitute.For<ICurrentUser>();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        var studentDirectory = Substitute.For<IStudentDirectory>();
        var aggregateCreator = Substitute.For<IAggregateExamAttemptCreator>();
        var auditService = Substitute.For<IAuditService>();

        var examId = Guid.NewGuid();
        var exam = new Exam { Id = examId, InstitutionId = Guid.NewGuid() };
        resourceAccess.ValidateExamAccessAsync(examId, Arg.Any<CancellationToken>()).Returns(exam);

        var handler = new CreateBulkManualResultHandler(db, currentUser, resourceAccess, studentDirectory, aggregateCreator, auditService);

        var items = new List<BulkStudentManualResultItemDto>();
        for (int i = 0; i < 501; i++)
        {
            items.Add(new BulkStudentManualResultItemDto(Guid.NewGuid(), []));
        }

        var command = new CreateBulkManualResultCommand(items);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(examId, command));
        Assert.Contains("BULK_LIMIT_EXCEEDED", ex.Message);
    }
}
