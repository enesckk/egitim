using ClosedXML.Excel;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.CreateManualResult;
using EgitimPlatform.Modules.Exams.Features.ExamResultImport;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Sprint5DIntegrationTests(IntegrationTestFactory factory)
{
    private class DummyAcademicCatalog : IAcademicCatalog
    {
        public Task<AcademicReferenceDto?> GetExamTypeAsync(Guid id, CancellationToken ct = default) => Task.FromResult<AcademicReferenceDto?>(null);
        public Task<IReadOnlyList<AcademicReferenceDto>> GetExamTypesAsync(IEnumerable<Guid> ids, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AcademicReferenceDto>>([]);
        public Task<bool> SubjectExistsAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateExamTypeSubjectAsync(Guid examTypeId, Guid subjectId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateSubjectTopicsAsync(Guid subjectId, IEnumerable<Guid> topicIds, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyDictionary<Guid, string>> GetSubjectNamesAsync(IEnumerable<Guid> subjectIds, CancellationToken ct = default)
        {
            var dict = subjectIds.ToDictionary(id => id, id => "Matematik");
            return Task.FromResult<IReadOnlyDictionary<Guid, string>>(dict);
        }
        public Task<IReadOnlyDictionary<Guid, string>> GetTopicNamesAsync(IEnumerable<Guid> topicIds, CancellationToken ct = default) => Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
        public Task<bool> ValidateTopicSubTopicAsync(Guid topicId, Guid subTopicId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> ValidateLearningOutcomeHierarchyAsync(Guid subjectId, Guid topicId, Guid? subTopicId, Guid learningOutcomeId, CancellationToken ct = default) => Task.FromResult(true);
    }

    private class TestCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => true;
        public IReadOnlyCollection<string> Roles => ["SuperAdmin"];
        public string? Email => "admin@test.local";
        public bool IsSuperAdmin => true;
        public bool IsInRole(string role) => role == "SuperAdmin";
        public Task<Guid?> GetInstitutionIdAsync() => Task.FromResult<Guid?>(null);
    }

    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(factory.ConnectionString).Options);

    private static string Code() => "S5D-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task SingleManualResult_HappyPath_CreatesFinalizedAggregateAttempt()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Manual Single" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-MAN1", FirstName = "Selim", LastName = "K" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Manual Single Exam", WrongAnswerPenalty = 4.0m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 10, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var scoringService = new ExamScoringService();
        var aggregateCreator = new AggregateExamAttemptCreator(scoringService);
        var studentDirectory = new StudentDirectory(db);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var handler = new CreateSingleManualResultHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, studentDirectory, aggregateCreator, Substitute.For<IAuditService>());

        var command = new CreateSingleManualResultCommand(
            student.Id,
            [ new ManualSectionResultInputDto(section.Id, 8, 2, 0) ]
        );

        // Act
        var response = await handler.HandleAsync(exam.Id, command);

        // Assert Response
        Assert.NotNull(response);
        Assert.Equal(student.Id, response.StudentId);
        Assert.Equal(exam.Id, response.ExamId);
        Assert.Equal(1, response.AttemptNumber);
        Assert.Equal(8, response.TotalCorrect);
        Assert.Equal(2, response.TotalWrong);
        Assert.Equal(0, response.TotalBlank);
        Assert.Equal(7.50m, response.TotalNet);
        Assert.False(response.IsRawAvailable);

        // Verify DB State
        using var checkDb = Db();
        var attempt = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.SubjectResults)
            .Include(a => a.TopicResults)
            .Include(a => a.QuestionResults)
            .SingleAsync(a => a.Id == response.AttemptId);

        Assert.Equal(AttemptStatus.Finalized, attempt.Status);
        Assert.NotNull(attempt.FinalizedAt);
        Assert.False(attempt.IsRawAvailable);

        Assert.Single(attempt.SubjectResults);
        Assert.Empty(attempt.TopicResults);
        Assert.Empty(attempt.QuestionResults);

        var sr = attempt.SubjectResults.Single();
        Assert.Equal(section.Id, sr.ExamSectionId);
        Assert.Equal(8, sr.Correct);
        Assert.Equal(2, sr.Wrong);
        Assert.Equal(0, sr.Blank);
        Assert.Equal(7.50m, sr.Net);
    }

    [Fact]
    public async Task SingleManualResult_Retake_IncrementsAttemptNumber()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Manual Retake" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-RET1", FirstName = "Derya", LastName = "L" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Manual Retake Exam", WrongAnswerPenalty = 4.0m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 10, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        // Seed Attempt 1
        var attempt1 = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = inst.Id,
            ExamId = exam.Id,
            StudentId = student.Id,
            AttemptNumber = 1,
            TakenAt = DateTimeOffset.UtcNow.AddDays(-1),
            Status = AttemptStatus.Finalized,
            FinalizedAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsRawAvailable = false,
            TotalCorrect = 5,
            TotalWrong = 5,
            TotalBlank = 0,
            TotalNet = 3.75m
        };
        db.Add(attempt1);
        await db.SaveChangesAsync();

        var scoringService = new ExamScoringService();
        var aggregateCreator = new AggregateExamAttemptCreator(scoringService);
        var studentDirectory = new StudentDirectory(db);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var handler = new CreateSingleManualResultHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, studentDirectory, aggregateCreator, Substitute.For<IAuditService>());

        var command = new CreateSingleManualResultCommand(
            student.Id,
            [ new ManualSectionResultInputDto(section.Id, 9, 1, 0) ]
        );

        // Act (Attempt 2)
        var response = await handler.HandleAsync(exam.Id, command);

        // Assert Response
        Assert.Equal(2, response.AttemptNumber);

        // Verify Both Attempts in DB
        using var checkDb = Db();
        var attempts = await checkDb.Set<StudentExamAttempt>()
            .Where(a => a.ExamId == exam.Id && a.StudentId == student.Id)
            .OrderBy(a => a.AttemptNumber)
            .ToListAsync();

        Assert.Equal(2, attempts.Count);
        Assert.Equal(1, attempts[0].AttemptNumber);
        Assert.Equal(3.75m, attempts[0].TotalNet);

        Assert.Equal(2, attempts[1].AttemptNumber);
        Assert.Equal(8.75m, attempts[1].TotalNet);
    }

    [Fact]
    public async Task BulkManualResult_HappyPath_CreatesAllStudentAttempts()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Bulk Manual" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-BLK1", FirstName = "Onur", LastName = "A" };
        var s2 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-BLK2", FirstName = "Buse", LastName = "B" };
        db.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Bulk Manual Exam", WrongAnswerPenalty = 4.0m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 5, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var scoringService = new ExamScoringService();
        var aggregateCreator = new AggregateExamAttemptCreator(scoringService);
        var studentDirectory = new StudentDirectory(db);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var handler = new CreateBulkManualResultHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, studentDirectory, aggregateCreator, Substitute.For<IAuditService>());

        var command = new CreateBulkManualResultCommand([
            new BulkStudentManualResultItemDto(s1.Id, [ new ManualSectionResultInputDto(section.Id, 5, 0, 0) ]),
            new BulkStudentManualResultItemDto(s2.Id, [ new ManualSectionResultInputDto(section.Id, 4, 1, 0) ])
        ]);

        // Act
        var response = await handler.HandleAsync(exam.Id, command);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(exam.Id, response.ExamId);
        Assert.Equal(2, response.StudentsProcessed);
        Assert.Equal(2, response.AttemptsCreated);
        Assert.Equal(2, response.SubjectResultsCreated);
        Assert.Equal(2, response.AttemptIds.Count);

        using var checkDb = Db();
        var attemptsCount = await checkDb.Set<StudentExamAttempt>().CountAsync(a => a.ExamId == exam.Id);
        Assert.Equal(2, attemptsCount);
    }

    [Fact]
    public async Task BulkManualResult_ValidationFailure_RollsBackAll()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Bulk Fail" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-FAIL1", FirstName = "Kemal", LastName = "X" };
        var s2 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-FAIL2", FirstName = "Lale", LastName = "Y" };
        db.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Bulk Fail Exam", WrongAnswerPenalty = 4.0m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 10, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var scoringService = new ExamScoringService();
        var aggregateCreator = new AggregateExamAttemptCreator(scoringService);
        var studentDirectory = new StudentDirectory(db);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var handler = new CreateBulkManualResultHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, studentDirectory, aggregateCreator, Substitute.For<IAuditService>());

        // Student 1 valid (10 questions), Student 2 invalid (total count 5 != 10)
        var command = new CreateBulkManualResultCommand([
            new BulkStudentManualResultItemDto(s1.Id, [ new ManualSectionResultInputDto(section.Id, 10, 0, 0) ]),
            new BulkStudentManualResultItemDto(s2.Id, [ new ManualSectionResultInputDto(section.Id, 3, 2, 0) ])
        ]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(exam.Id, command));
        Assert.Contains("QUESTION_COUNT_MISMATCH", ex.Message);

        using var checkDb = Db();
        var attemptsCount = await checkDb.Set<StudentExamAttempt>().CountAsync(a => a.ExamId == exam.Id);
        Assert.Equal(0, attemptsCount);
    }

    [Fact]
    public async Task BulkManualResult_DuplicateStudent_RejectsRequest()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Dup Student" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-DUPST1", FirstName = "Tarik", LastName = "Z" };
        db.Add(s1);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Dup Student Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 5, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var scoringService = new ExamScoringService();
        var aggregateCreator = new AggregateExamAttemptCreator(scoringService);
        var studentDirectory = new StudentDirectory(db);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var handler = new CreateBulkManualResultHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, studentDirectory, aggregateCreator, Substitute.For<IAuditService>());

        var command = new CreateBulkManualResultCommand([
            new BulkStudentManualResultItemDto(s1.Id, [ new ManualSectionResultInputDto(section.Id, 5, 0, 0) ]),
            new BulkStudentManualResultItemDto(s1.Id, [ new ManualSectionResultInputDto(section.Id, 4, 1, 0) ])
        ]);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(exam.Id, command));
        Assert.Contains("DUPLICATE_STUDENT_RESULT", ex.Message);
    }

    [Fact]
    public async Task Parity_ManualEntryVsExcelAggregate_ProducesIdenticalResults()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Parity Manual Excel" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-PAR-EX1", FirstName = "Emin", LastName = "P1" };
        var s2 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-PAR-MA2", FirstName = "Canan", LastName = "P2" };
        db.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Parity Aggregate Exam", WrongAnswerPenalty = 4.0m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 10, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var scoringService = new ExamScoringService();
        var aggregateCreator = new AggregateExamAttemptCreator(scoringService);
        var studentDirectory = new StudentDirectory(db);
        var validator = new ImportFileValidator();

        // 1. Excel Aggregate Import for Student 1 (Correct 8, Wrong 2, Blank 0)
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Correct";
        ws.Cell(1, 5).Value = $"SEC_{section.Id}_Wrong";
        ws.Cell(1, 6).Value = $"SEC_{section.Id}_Blank";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-PAR-EX1"; ws.Cell(2, 3).Value = "Emin P1";
        ws.Cell(2, 4).Value = 8; ws.Cell(2, 5).Value = 2; ws.Cell(2, 6).Value = 0;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "parity_agg.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);
        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.AggregateResults, file);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>(), aggregateCreator);

        await commitHandler.HandleAsync(exam.Id, ImportType.AggregateResults, preview.FileHash, file);

        // 2. Manual Entry for Student 2 with identical values (Correct 8, Wrong 2, Blank 0)
        var singleHandler = new CreateSingleManualResultHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, studentDirectory, aggregateCreator, Substitute.For<IAuditService>());

        var manualResponse = await singleHandler.HandleAsync(exam.Id, new CreateSingleManualResultCommand(
            s2.Id, [ new ManualSectionResultInputDto(section.Id, 8, 2, 0) ]));

        // 3. Compare Database Entities
        using var checkDb = Db();
        var excelAttempt = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.SubjectResults)
            .SingleAsync(a => a.ExamId == exam.Id && a.StudentId == s1.Id);

        var manualAttempt = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.SubjectResults)
            .SingleAsync(a => a.Id == manualResponse.AttemptId);

        Assert.Equal(excelAttempt.TotalCorrect, manualAttempt.TotalCorrect);
        Assert.Equal(excelAttempt.TotalWrong, manualAttempt.TotalWrong);
        Assert.Equal(excelAttempt.TotalBlank, manualAttempt.TotalBlank);
        Assert.Equal(excelAttempt.TotalNet, manualAttempt.TotalNet);
        Assert.Equal(excelAttempt.Status, manualAttempt.Status);
        Assert.Equal(excelAttempt.IsRawAvailable, manualAttempt.IsRawAvailable);

        var excelSr = excelAttempt.SubjectResults.Single();
        var manualSr = manualAttempt.SubjectResults.Single();

        Assert.Equal(excelSr.Correct, manualSr.Correct);
        Assert.Equal(excelSr.Wrong, manualSr.Wrong);
        Assert.Equal(excelSr.Blank, manualSr.Blank);
        Assert.Equal(excelSr.Net, manualSr.Net);
    }

    [Fact]
    public async Task SingleManualResult_ConcurrentAttemptCreation_EnforcesUniqueIndex()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Concurrent Test" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-CONC1", FirstName = "Hakan", LastName = "C" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Concurrent Manual Exam", WrongAnswerPenalty = 4.0m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 10, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var scoringService = new ExamScoringService();
        var aggregateCreator = new AggregateExamAttemptCreator(scoringService);
        var studentDirectory1 = new StudentDirectory(Db());
        var studentDirectory2 = new StudentDirectory(Db());

        var resourceAccess1 = Substitute.For<IExamResourceAccess>();
        resourceAccess1.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var resourceAccess2 = Substitute.For<IExamResourceAccess>();
        resourceAccess2.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var handler1 = new CreateSingleManualResultHandler(Db(), new TestCurrentUser(Guid.NewGuid()), resourceAccess1, studentDirectory1, aggregateCreator, Substitute.For<IAuditService>());
        var handler2 = new CreateSingleManualResultHandler(Db(), new TestCurrentUser(Guid.NewGuid()), resourceAccess2, studentDirectory2, aggregateCreator, Substitute.For<IAuditService>());

        var command1 = new CreateSingleManualResultCommand(student.Id, [ new ManualSectionResultInputDto(section.Id, 8, 2, 0) ]);
        var command2 = new CreateSingleManualResultCommand(student.Id, [ new ManualSectionResultInputDto(section.Id, 7, 3, 0) ]);

        // Act: Execute two concurrent attempt requests
        var task1 = handler1.HandleAsync(exam.Id, command1);
        var task2 = handler2.HandleAsync(exam.Id, command2);

        try
        {
            await Task.WhenAll(task1, task2);
        }
        catch
        {
            // One task may throw ConflictException due to DB unique index collision
        }

        // Assert DB integrity: No duplicate (ExamId, StudentId, AttemptNumber) exists in DB
        using var checkDb = Db();
        var attempts = await checkDb.Set<StudentExamAttempt>()
            .Where(a => a.ExamId == exam.Id && a.StudentId == student.Id)
            .OrderBy(a => a.AttemptNumber)
            .ToListAsync();

        Assert.True(attempts.Count is 1 or 2);
        var attemptNumbers = attempts.Select(a => a.AttemptNumber).ToList();
        Assert.Equal(attemptNumbers.Distinct().Count(), attempts.Count);
    }
}
