using ClosedXML.Excel;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.ExamResultImport;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Sprint5CIntegrationTests(IntegrationTestFactory factory)
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

    private static string Code() => "S5C-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task CommitResultImport_QuestionAnswers_HappyPath_CreatesAttemptsAndRawResults()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst QA Commit Happy" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-QA1", FirstName = "Ahmet", LastName = "K" };
        var s2 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-QA2", FirstName = "Zeynep", LastName = "L" };
        db.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT QA" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "QA Commit Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 2, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Cebir" };
        db.Add(topic);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1 Content" };
        var q2 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q2 Content" };
        db.AddRange(q1, q2);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, CorrectOption = "A" };
        var eq2 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q2.Id, QuestionNumber = 2, CorrectOption = "B" };
        db.AddRange(eq1, eq2);
        await db.SaveChangesAsync();

        // Build Excel workbook with EQ machine columns
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"EQ_{eq1.Id}"; ws.Cell(1, 5).Value = $"EQ_{eq2.Id}";

        // Student 1: A (Correct), B (Correct) -> 2 correct, 0 wrong
        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-QA1"; ws.Cell(2, 3).Value = "Ahmet K";
        ws.Cell(2, 4).Value = "A"; ws.Cell(2, 5).Value = "B";

        // Student 2: A (Correct), C (Wrong) -> 1 correct, 1 wrong
        ws.Cell(3, 1).Value = s2.Id.ToString(); ws.Cell(3, 2).Value = "STD-QA2"; ws.Cell(3, 3).Value = "Zeynep L";
        ws.Cell(3, 4).Value = "A"; ws.Cell(3, 5).Value = "C";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "qa_commit.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var rawFinalizer = new RawExamAttemptFinalizer();
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.QuestionAnswers, file);

        Assert.True(preview.CanImport);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var currentUser = new TestCurrentUser(Guid.NewGuid());
        var commitHandler = new CommitResultImportHandler(db, currentUser, resourceAccess, previewEngine, scoringService, rawFinalizer, Substitute.For<IAuditService>());

        // Act
        var result = await commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, preview.FileHash, file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(exam.Id, result.ExamId);
        Assert.Equal(preview.FileHash, result.FileHash);
        Assert.Equal(2, result.ImportedStudentsCount);
        Assert.Equal(2, result.CreatedAttemptsCount);

        // Verify Database Records
        using var checkDb = Db();
        var attempts = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.SubjectResults)
            .Include(a => a.TopicResults)
            .Include(a => a.QuestionResults)
            .Where(a => a.ExamId == exam.Id)
            .OrderBy(a => a.StudentId)
            .ToListAsync();

        Assert.Equal(2, attempts.Count);
        foreach (var att in attempts)
        {
            Assert.Equal(AttemptStatus.Finalized, att.Status);
            Assert.NotNull(att.FinalizedAt);
            Assert.True(att.IsRawAvailable);
            Assert.Equal(2, att.QuestionResults.Count);
            Assert.Single(att.SubjectResults);
            Assert.Equal(att.TotalCorrect + att.TotalWrong + att.TotalBlank, att.QuestionResults.Count);
        }

        var s1Attempt = attempts.First(a => a.StudentId == s1.Id);
        Assert.Equal(2, s1Attempt.TotalCorrect);
        Assert.Equal(0, s1Attempt.TotalWrong);
        Assert.Equal(2.0m, s1Attempt.TotalNet);

        var s2Attempt = attempts.First(a => a.StudentId == s2.Id);
        Assert.Equal(1, s2Attempt.TotalCorrect);
        Assert.Equal(1, s2Attempt.TotalWrong);
        Assert.Equal(0.75m, s2Attempt.TotalNet); // 1 - 1/4 = 0.75

        // Verify Audit Ledger Record
        var ledger = await checkDb.Set<ExamResultImport>()
            .FirstOrDefaultAsync(e => e.ExamId == exam.Id && e.FileHash == preview.FileHash);

        Assert.NotNull(ledger);
        Assert.Equal(ImportType.QuestionAnswers, ledger.ImportType);
        Assert.Equal(ImportStatus.Completed, ledger.Status);
        Assert.Equal(2, ledger.ImportedStudentsCount);
    }

    [Fact]
    public async Task CommitResultImport_QuestionAnswers_PartialBlank_CreatesBlankResults()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Blank QA" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-BLK", FirstName = "Murat", LastName = "P" };
        db.Add(s1);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Blank Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 2, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var topic = new Topic { SubjectId = subject.Id, Code = Code(), Name = "Cebir" };
        db.Add(topic);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q1" };
        var q2 = new Question { InstitutionId = inst.Id, SubjectId = subject.Id, TopicId = topic.Id, Content = "Q2" };
        db.AddRange(q1, q2);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q1.Id, QuestionNumber = 1, CorrectOption = "A" };
        var eq2 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = section.Id, QuestionId = q2.Id, QuestionNumber = 2, CorrectOption = "B" };
        db.AddRange(eq1, eq2);
        await db.SaveChangesAsync();

        // Student 1: A (Correct), blank cell for EQ2
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"EQ_{eq1.Id}"; ws.Cell(1, 5).Value = $"EQ_{eq2.Id}";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-BLK"; ws.Cell(2, 3).Value = "Murat P";
        ws.Cell(2, 4).Value = "A"; ws.Cell(2, 5).Value = "";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "blank_qa.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.QuestionAnswers, file);
        Assert.True(preview.CanImport);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>());

        await commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, preview.FileHash, file);

        using var checkDb = Db();
        var attempt = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.QuestionResults)
            .SingleAsync(a => a.ExamId == exam.Id && a.StudentId == s1.Id);

        Assert.Equal(1, attempt.TotalCorrect);
        Assert.Equal(0, attempt.TotalWrong);
        Assert.Equal(1, attempt.TotalBlank);
        Assert.Equal(1.0m, attempt.TotalNet);

        var eq2Result = attempt.QuestionResults.First(r => r.ExamQuestionId == eq2.Id);
        Assert.True(eq2Result.IsBlank);
        Assert.Null(eq2Result.IsCorrect);
        Assert.Equal(0m, eq2Result.NetContribution);
    }

    [Fact]
    public async Task CommitResultImport_QuestionAnswers_TamperedQuestion_RejectsImport()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Tampered" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-TMP", FirstName = "Omer", LastName = "T" };
        db.Add(s1);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Tampered Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var foreignExamQuestionId = Guid.NewGuid();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"EQ_{foreignExamQuestionId}";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-TMP"; ws.Cell(2, 3).Value = "Omer T";
        ws.Cell(2, 4).Value = "A";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "tampered.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.QuestionAnswers, file);

        Assert.False(preview.CanImport);
        var issue = preview.Issues.FirstOrDefault(i => i.Code == ImportIssueCodes.QuestionNumberNotFound);
        Assert.NotNull(issue);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>());

        await Assert.ThrowsAsync<ValidationException>(() =>
            commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, preview.FileHash, file));

        using var checkDb = Db();
        var attemptsCount = await checkDb.Set<StudentExamAttempt>().CountAsync(a => a.ExamId == exam.Id);
        Assert.Equal(0, attemptsCount);
    }

    [Fact]
    public async Task CommitResultImport_QuestionAnswers_SectionPenaltyOverride_CalculatesNetContributionsCorrectly()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Section Penalty" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-PEN1", FirstName = "Can", LastName = "V" };
        db.Add(s1);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var sub1 = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        var sub2 = new Subject { Code = Code(), Name = "Fizik", ExamTypeId = examType.Id };
        db.AddRange(examType, sub1, sub2);
        await db.SaveChangesAsync();

        // Exam default penalty = 4 (1/4 = 0.25)
        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Penalty Exam", WrongAnswerPenalty = 4.0m };
        db.Add(exam);
        await db.SaveChangesAsync();

        // Section A (Matematik) penalty override = 3 (1/3 = 0.3333)
        var secA = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = sub1.Id, QuestionCount = 1, DisplayOrder = 1, WrongAnswerPenalty = 3.0m };
        // Section B (Fizik) penalty override = null (uses Exam penalty = 4 -> 1/4 = 0.25)
        var secB = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = sub2.Id, QuestionCount = 1, DisplayOrder = 2, WrongAnswerPenalty = null };
        db.AddRange(secA, secB);
        await db.SaveChangesAsync();

        var top1 = new Topic { SubjectId = sub1.Id, Code = Code(), Name = "TopA" };
        var top2 = new Topic { SubjectId = sub2.Id, Code = Code(), Name = "TopB" };
        db.AddRange(top1, top2);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = sub1.Id, TopicId = top1.Id, Content = "QA" };
        var q2 = new Question { InstitutionId = inst.Id, SubjectId = sub2.Id, TopicId = top2.Id, Content = "QB" };
        db.AddRange(q1, q2);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = secA.Id, QuestionId = q1.Id, QuestionNumber = 1, CorrectOption = "A" };
        var eq2 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = secB.Id, QuestionId = q2.Id, QuestionNumber = 2, CorrectOption = "A" };
        db.AddRange(eq1, eq2);
        await db.SaveChangesAsync();

        // Student 1 answers B (Wrong for eq1 in SecA) and B (Wrong for eq2 in SecB)
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"EQ_{eq1.Id}"; ws.Cell(1, 5).Value = $"EQ_{eq2.Id}";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-PEN1"; ws.Cell(2, 3).Value = "Can V";
        ws.Cell(2, 4).Value = "B"; ws.Cell(2, 5).Value = "B";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "penalty.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.QuestionAnswers, file);
        Assert.True(preview.CanImport);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>());

        await commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, preview.FileHash, file);

        using var checkDb = Db();
        var attempt = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.QuestionResults)
            .Include(a => a.SubjectResults)
            .SingleAsync(a => a.ExamId == exam.Id && a.StudentId == s1.Id);

        var r1 = attempt.QuestionResults.First(r => r.ExamQuestionId == eq1.Id);
        var r2 = attempt.QuestionResults.First(r => r.ExamQuestionId == eq2.Id);

        Assert.Equal(-0.33m, r1.NetContribution); // 1/3 penalty rounded to 2 decimals per database column precision (5,2)
        Assert.Equal(-0.25m, r2.NetContribution); // 1/4 penalty rounded to 2 decimals per database column precision (5,2)

        var srA = attempt.SubjectResults.First(s => s.ExamSectionId == secA.Id);
        var srB = attempt.SubjectResults.First(s => s.ExamSectionId == secB.Id);

        Assert.Equal(-0.33m, srA.Net); // CalculateNet rounds subject net to 2 decimal places
        Assert.Equal(-0.25m, srB.Net); // CalculateNet rounds subject net to 2 decimal places
        Assert.Equal(-0.58m, attempt.TotalNet);
    }

    [Fact]
    public async Task CommitResultImport_QuestionAnswers_ParityWithNormalFinalize_ProducesIdenticalResults()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Parity" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-PAR1", FirstName = "Eda", LastName = "M" };
        var s2 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-PAR2", FirstName = "Ali", LastName = "N" };
        db.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var sub = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, sub);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Parity Exam", WrongAnswerPenalty = 4.0m };
        db.Add(exam);
        await db.SaveChangesAsync();

        var sec = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = sub.Id, QuestionCount = 2, DisplayOrder = 1 };
        db.Add(sec);
        await db.SaveChangesAsync();

        var top = new Topic { SubjectId = sub.Id, Code = Code(), Name = "ParityTopic" };
        db.Add(top);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = sub.Id, TopicId = top.Id, Content = "Q1" };
        var q2 = new Question { InstitutionId = inst.Id, SubjectId = sub.Id, TopicId = top.Id, Content = "Q2" };
        db.AddRange(q1, q2);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = sec.Id, QuestionId = q1.Id, QuestionNumber = 1, CorrectOption = "A" };
        var eq2 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = sec.Id, QuestionId = q2.Id, QuestionNumber = 2, CorrectOption = "B" };
        db.AddRange(eq1, eq2);
        await db.SaveChangesAsync();

        // 1. Normal Finalize for Student 1 (A: Correct, C: Wrong)
        var attemptNormal = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = inst.Id,
            ExamId = exam.Id,
            StudentId = s1.Id,
            AttemptNumber = 1,
            TakenAt = DateTimeOffset.UtcNow,
            Status = AttemptStatus.Draft
        };
        db.Add(attemptNormal);
        await db.SaveChangesAsync();

        var ans1 = new StudentExamQuestionResult { Id = Guid.NewGuid(), InstitutionId = inst.Id, StudentExamAttemptId = attemptNormal.Id, ExamQuestionId = eq1.Id, QuestionId = q1.Id, SelectedOption = "A", IsBlank = false };
        var ans2 = new StudentExamQuestionResult { Id = Guid.NewGuid(), InstitutionId = inst.Id, StudentExamAttemptId = attemptNormal.Id, ExamQuestionId = eq2.Id, QuestionId = q2.Id, SelectedOption = "C", IsBlank = false };
        db.AddRange(ans1, ans2);
        await db.SaveChangesAsync();

        var scoringService = new ExamScoringService();
        var rawFinalizer = new RawExamAttemptFinalizer();
        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateAttemptWriteAccessAsync(attemptNormal.Id, Arg.Any<CancellationToken>()).Returns(attemptNormal);

        var finalizeHandler = new EgitimPlatform.Modules.Exams.Features.FinalizeAttempt.FinalizeAttemptHandler(
            db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, scoringService, rawFinalizer, Substitute.For<IAuditService>());

        await finalizeHandler.HandleAsync(attemptNormal.Id);

        // 2. Excel Import Commit for Student 2 with identical answers (A: Correct, C: Wrong)
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"EQ_{eq1.Id}"; ws.Cell(1, 5).Value = $"EQ_{eq2.Id}";

        ws.Cell(2, 1).Value = s2.Id.ToString(); ws.Cell(2, 2).Value = "STD-PAR2"; ws.Cell(2, 3).Value = "Ali N";
        ws.Cell(2, 4).Value = "A"; ws.Cell(2, 5).Value = "C";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "parity.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.QuestionAnswers, file);

        var examResourceAccess = Substitute.For<IExamResourceAccess>();
        examResourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), examResourceAccess, previewEngine, scoringService, rawFinalizer, Substitute.For<IAuditService>());

        await commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, preview.FileHash, file);

        // 3. Compare Both Attempts in DB
        using var checkDb = Db();
        var normalInDb = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.QuestionResults)
            .Include(a => a.SubjectResults)
            .Include(a => a.TopicResults)
            .SingleAsync(a => a.Id == attemptNormal.Id);

        var importedInDb = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.QuestionResults)
            .Include(a => a.SubjectResults)
            .Include(a => a.TopicResults)
            .SingleAsync(a => a.ExamId == exam.Id && a.StudentId == s2.Id);

        Assert.Equal(normalInDb.TotalCorrect, importedInDb.TotalCorrect);
        Assert.Equal(normalInDb.TotalWrong, importedInDb.TotalWrong);
        Assert.Equal(normalInDb.TotalBlank, importedInDb.TotalBlank);
        Assert.Equal(normalInDb.TotalNet, importedInDb.TotalNet);
        Assert.Equal(normalInDb.Status, importedInDb.Status);
        Assert.Equal(normalInDb.IsRawAvailable, importedInDb.IsRawAvailable);

        var q1Normal = normalInDb.QuestionResults.First(r => r.ExamQuestionId == eq1.Id);
        var q1Imported = importedInDb.QuestionResults.First(r => r.ExamQuestionId == eq1.Id);
        Assert.Equal(q1Normal.IsCorrect, q1Imported.IsCorrect);
        Assert.Equal(q1Normal.IsBlank, q1Imported.IsBlank);
        Assert.Equal(q1Normal.NetContribution, q1Imported.NetContribution);

        var q2Normal = normalInDb.QuestionResults.First(r => r.ExamQuestionId == eq2.Id);
        var q2Imported = importedInDb.QuestionResults.First(r => r.ExamQuestionId == eq2.Id);
        Assert.Equal(q2Normal.IsCorrect, q2Imported.IsCorrect);
        Assert.Equal(q2Normal.IsBlank, q2Imported.IsBlank);
        Assert.Equal(q2Normal.NetContribution, q2Imported.NetContribution);

        var srNormal = normalInDb.SubjectResults.Single();
        var srImported = importedInDb.SubjectResults.Single();
        Assert.Equal(srNormal.Correct, srImported.Correct);
        Assert.Equal(srNormal.Wrong, srImported.Wrong);
        Assert.Equal(srNormal.Blank, srImported.Blank);
        Assert.Equal(srNormal.Net, srImported.Net);

        var trNormal = normalInDb.TopicResults.Single();
        var trImported = importedInDb.TopicResults.Single();
        Assert.Equal(trNormal.Correct, trImported.Correct);
        Assert.Equal(trNormal.Wrong, trImported.Wrong);
        Assert.Equal(trNormal.Blank, trImported.Blank);
        Assert.Equal(trNormal.Net, trImported.Net);
    }

    [Fact]
    public async Task CommitResultImport_QuestionAnswers_ArchivedQuestion_FinalizesAndScoresCorrectly()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Archived Q" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-ARC1", FirstName = "Hasan", LastName = "B" };
        db.Add(s1);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var sub = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, sub);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Archived Q Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var sec = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = sub.Id, QuestionCount = 1, DisplayOrder = 1 };
        db.Add(sec);
        await db.SaveChangesAsync();

        var top = new Topic { SubjectId = sub.Id, Code = Code(), Name = "ArchivedTop" };
        db.Add(top);
        await db.SaveChangesAsync();

        // Question created and then archived
        var q1 = new Question { InstitutionId = inst.Id, SubjectId = sub.Id, TopicId = top.Id, Content = "Q1 Content", Status = QuestionStatus.Archived };
        db.Add(q1);
        await db.SaveChangesAsync();

        // ExamQuestion linked with snapshot CorrectOption = "C"
        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = sec.Id, QuestionId = q1.Id, QuestionNumber = 1, CorrectOption = "C" };
        db.Add(eq1);
        await db.SaveChangesAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"EQ_{eq1.Id}";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-ARC1"; ws.Cell(2, 3).Value = "Hasan B";
        ws.Cell(2, 4).Value = "C"; // Correct answer

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "archived_q.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.QuestionAnswers, file);
        Assert.True(preview.CanImport);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>());

        await commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, preview.FileHash, file);

        using var checkDb = Db();
        var attempt = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.QuestionResults)
            .SingleAsync(a => a.ExamId == exam.Id && a.StudentId == s1.Id);

        Assert.Equal(1, attempt.TotalCorrect);
        Assert.Equal(1.0m, attempt.TotalNet);
        var qRes = attempt.QuestionResults.Single();
        Assert.True(qRes.IsCorrect);
        Assert.Equal(1.0m, qRes.NetContribution);
    }

    [Fact]
    public async Task CommitResultImport_QuestionAnswers_DuplicateCommit_ThrowsConflictException()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Duplicate" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-DUP1", FirstName = "Gizem", LastName = "S" };
        db.Add(s1);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var sub = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, sub);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Duplicate Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var sec = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = sub.Id, QuestionCount = 1, DisplayOrder = 1 };
        db.Add(sec);
        await db.SaveChangesAsync();

        var top = new Topic { SubjectId = sub.Id, Code = Code(), Name = "DupTop" };
        db.Add(top);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = sub.Id, TopicId = top.Id, Content = "Q1 Content" };
        db.Add(q1);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = sec.Id, QuestionId = q1.Id, QuestionNumber = 1, CorrectOption = "A" };
        db.Add(eq1);
        await db.SaveChangesAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"EQ_{eq1.Id}";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-DUP1"; ws.Cell(2, 3).Value = "Gizem S";
        ws.Cell(2, 4).Value = "A";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "dup.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.QuestionAnswers, file);
        Assert.True(preview.CanImport);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>());

        // 1st Commit: Succeeds
        var res1 = await commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, preview.FileHash, file);
        Assert.NotNull(res1);

        // 2nd Commit: Throws ConflictException
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, preview.FileHash, file));

        Assert.Contains("IMPORT_ALREADY_COMPLETED", ex.Message);
    }

    private class FailingDbContextDecorator(IApplicationDbContext inner, int failOnSaveCallNumber) : IApplicationDbContext
    {
        public int SaveChangesCount { get; private set; }
        public int BeginTransactionCount { get; private set; }

        public DbSet<TEntity> Set<TEntity>() where TEntity : class => inner.Set<TEntity>();

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            BeginTransactionCount++;
            return inner.BeginTransactionAsync(cancellationToken);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            if (SaveChangesCount == failOnSaveCallNumber)
            {
                throw new InvalidOperationException($"FORCED_TEST_FAILURE: Simulating database failure on SaveChangesAsync call #{failOnSaveCallNumber}.");
            }
            return await inner.SaveChangesAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task CommitResultImport_QuestionAnswers_CrossBatchRollback_RevertsBatch1AndBatch2OnFailure()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Cross Batch" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-CB1", FirstName = "Ali", LastName = "B1" };
        var s2 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-CB2", FirstName = "Veli", LastName = "B1" };
        var s3 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-CB3", FirstName = "Ayse", LastName = "B2" };
        var s4 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-CB4", FirstName = "Fatma", LastName = "B2" };
        db.AddRange(s1, s2, s3, s4);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var sub = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, sub);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Cross Batch Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var sec = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = sub.Id, QuestionCount = 1, DisplayOrder = 1 };
        db.Add(sec);
        await db.SaveChangesAsync();

        var top = new Topic { SubjectId = sub.Id, Code = Code(), Name = "CBTop" };
        db.Add(top);
        await db.SaveChangesAsync();

        var q1 = new Question { InstitutionId = inst.Id, SubjectId = sub.Id, TopicId = top.Id, Content = "Q1 Content" };
        db.Add(q1);
        await db.SaveChangesAsync();

        var eq1 = new ExamQuestion { InstitutionId = inst.Id, ExamId = exam.Id, ExamSectionId = sec.Id, QuestionId = q1.Id, QuestionNumber = 1, CorrectOption = "A" };
        db.Add(eq1);
        await db.SaveChangesAsync();

        // 4 Students -> With BatchSize = 2, Batch 1 = Students 1 & 2 (Save #1), Batch 2 = Students 3 & 4 (Save #2)
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"EQ_{eq1.Id}";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-CB1"; ws.Cell(2, 3).Value = "Ali B1"; ws.Cell(2, 4).Value = "A";
        ws.Cell(3, 1).Value = s2.Id.ToString(); ws.Cell(3, 2).Value = "STD-CB2"; ws.Cell(3, 3).Value = "Veli B1"; ws.Cell(3, 4).Value = "A";
        ws.Cell(4, 1).Value = s3.Id.ToString(); ws.Cell(4, 2).Value = "STD-CB3"; ws.Cell(4, 3).Value = "Ayse B2"; ws.Cell(4, 4).Value = "A";
        ws.Cell(5, 1).Value = s4.Id.ToString(); ws.Cell(5, 2).Value = "STD-CB4"; ws.Cell(5, 3).Value = "Fatma B2"; ws.Cell(5, 4).Value = "A";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "cross_batch.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.QuestionAnswers, file);
        Assert.True(preview.CanImport);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        // Fail on 2nd SaveChanges call (Batch 2) after Batch 1 has executed SaveChanges
        var failingDb = new FailingDbContextDecorator(db, failOnSaveCallNumber: 2);

        var commitHandler = new CommitResultImportHandler(
            failingDb,
            new TestCurrentUser(Guid.NewGuid()),
            resourceAccess,
            previewEngine,
            scoringService,
            new RawExamAttemptFinalizer(),
            Substitute.For<IAuditService>(),
            batchSize: 2);

        // Act & Assert Exception
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, preview.FileHash, file));

        Assert.Contains("FORCED_TEST_FAILURE", ex.Message);
        Assert.Equal(1, failingDb.BeginTransactionCount);
        Assert.Equal(2, failingDb.SaveChangesCount);

        // Assert Database State with a Fresh DbContext (bypassing EF local tracking cache)
        using var checkDb = Db();
        var attemptsCount = await checkDb.Set<StudentExamAttempt>().CountAsync(a => a.ExamId == exam.Id);
        var qResultsCount = await checkDb.Set<StudentExamQuestionResult>().CountAsync(r => checkDb.Set<StudentExamAttempt>().Any(a => a.ExamId == exam.Id && a.Id == r.StudentExamAttemptId));
        var sResultsCount = await checkDb.Set<StudentExamSubjectResult>().CountAsync(r => checkDb.Set<StudentExamAttempt>().Any(a => a.ExamId == exam.Id && a.Id == r.StudentExamAttemptId));
        var tResultsCount = await checkDb.Set<StudentExamTopicResult>().CountAsync(r => checkDb.Set<StudentExamAttempt>().Any(a => a.ExamId == exam.Id && a.Id == r.StudentExamAttemptId));
        var importLedgerCount = await checkDb.Set<ExamResultImport>().CountAsync(e => e.ExamId == exam.Id);

        Assert.Equal(0, attemptsCount);
        Assert.Equal(0, qResultsCount);
        Assert.Equal(0, sResultsCount);
        Assert.Equal(0, tResultsCount);
        Assert.Equal(0, importLedgerCount);
    }
}

