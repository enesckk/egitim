using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Content.Features.ManageMaterials;
using EgitimPlatform.Modules.Content.Services;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.ExamResultImport;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Identity.Entities;
using EgitimPlatform.Modules.Identity.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using EgitimPlatform.Modules.Teachers.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Sprint5BIntegrationTests(IntegrationTestFactory factory)
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

    private static string Code() => "S5B-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task CommitResultImport_HappyPath_CreatesFinalizedAttemptsAndLedger()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Commit Happy" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-A1", FirstName = "Ali", LastName = "Y" };
        var s2 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-A2", FirstName = "Ayse", LastName = "K" };
        db.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Commit Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 40, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        // Build Excel workbook
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId";
        ws.Cell(1, 2).Value = "StudentNumber";
        ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Correct";
        ws.Cell(1, 5).Value = $"SEC_{section.Id}_Wrong";
        ws.Cell(1, 6).Value = $"SEC_{section.Id}_Blank";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-A1"; ws.Cell(2, 3).Value = "Ali Y";
        ws.Cell(2, 4).Value = 30; ws.Cell(2, 5).Value = 8; ws.Cell(2, 6).Value = 2;

        ws.Cell(3, 1).Value = s2.Id.ToString(); ws.Cell(3, 2).Value = "STD-A2"; ws.Cell(3, 3).Value = "Ayse K";
        ws.Cell(3, 4).Value = 35; ws.Cell(3, 5).Value = 4; ws.Cell(3, 6).Value = 1;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "commit.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);

        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);
        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.AggregateResults, file);

        Assert.True(preview.CanImport);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var auditService = Substitute.For<IAuditService>();
        var currentUser = new TestCurrentUser(Guid.NewGuid());

        var commitHandler = new CommitResultImportHandler(db, currentUser, resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), auditService);

        // Act
        var result = await commitHandler.HandleAsync(exam.Id, ImportType.AggregateResults, preview.FileHash, file);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(exam.Id, result.ExamId);
        Assert.Equal(preview.FileHash, result.FileHash);
        Assert.Equal(2, result.ImportedStudentsCount);
        Assert.Equal(2, result.CreatedAttemptsCount);

        // Verify Database persistence
        using var checkDb = Db();
        var attempts = await checkDb.Set<StudentExamAttempt>()
            .Include(a => a.SubjectResults)
            .Include(a => a.TopicResults)
            .Include(a => a.QuestionResults)
            .Where(a => a.ExamId == exam.Id)
            .ToListAsync();

        Assert.Equal(2, attempts.Count);
        foreach (var att in attempts)
        {
            Assert.Equal(AttemptStatus.Finalized, att.Status);
            Assert.NotNull(att.FinalizedAt);
            Assert.False(att.IsRawAvailable);
            Assert.Single(att.SubjectResults);
            Assert.Empty(att.TopicResults); // No fabricated topic results
            Assert.Empty(att.QuestionResults);
        }

        // Verify Audit Ledger Entry
        var ledger = await checkDb.Set<ExamResultImport>()
            .FirstOrDefaultAsync(e => e.ExamId == exam.Id && e.FileHash == preview.FileHash);

        Assert.NotNull(ledger);
        Assert.Equal(ImportStatus.Completed, ledger.Status);
        Assert.Equal(2, ledger.ImportedStudentsCount);
    }

    [Fact]
    public async Task CommitResultImport_DuplicateCommit_ThrowsConflictException()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Duplicate Commit" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-DUP", FirstName = "Deniz", LastName = "B" };
        db.Add(s1);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Dup Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 20, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Correct"; ws.Cell(1, 5).Value = $"SEC_{section.Id}_Wrong"; ws.Cell(1, 6).Value = $"SEC_{section.Id}_Blank";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-DUP"; ws.Cell(2, 3).Value = "Deniz B";
        ws.Cell(2, 4).Value = 15; ws.Cell(2, 5).Value = 5; ws.Cell(2, 6).Value = 0;

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

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.AggregateResults, file);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>());

        // 1st Commit: Success
        await commitHandler.HandleAsync(exam.Id, ImportType.AggregateResults, preview.FileHash, file);

        // 2nd Commit of exact same file: ConflictException
        var file2 = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "dup.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        await Assert.ThrowsAsync<ConflictException>(() =>
            commitHandler.HandleAsync(exam.Id, ImportType.AggregateResults, preview.FileHash, file2));
    }

    [Fact]
    public async Task CommitResultImport_QuestionAnswersScope_ThrowsValidationException()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst QA" };
        var examType = new ExamType { Code = Code(), Name = "TYT QA" };
        db.AddRange(inst, examType);
        await db.SaveChangesAsync();

        var exam = new Exam { Id = Guid.NewGuid(), InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "QA Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var file = Substitute.For<IFormFile>();
        var scoringService = new ExamScoringService();
        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, Substitute.For<IExamResultImportPreviewEngine>(), scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            commitHandler.HandleAsync(exam.Id, ImportType.QuestionAnswers, "", file));

        Assert.Contains("fileHash is required", ex.Message);
    }

    [Fact]
    public async Task CommitResultImport_AllOrNothing_InvalidRow_ZeroWrites()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst AllOrNothing" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-AON1", FirstName = "Can", LastName = "A" };
        var s2 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-AON2", FirstName = "Ece", LastName = "B" };
        db.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "AON Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 40, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        // Workbook: Row 1 valid, Row 2 INVALID question count (30+0+0 = 30 != 40)
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Correct"; ws.Cell(1, 5).Value = $"SEC_{section.Id}_Wrong"; ws.Cell(1, 6).Value = $"SEC_{section.Id}_Blank";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-AON1"; ws.Cell(2, 3).Value = "Can A";
        ws.Cell(2, 4).Value = 30; ws.Cell(2, 5).Value = 8; ws.Cell(2, 6).Value = 2; // Valid (40)

        ws.Cell(3, 1).Value = s2.Id.ToString(); ws.Cell(3, 2).Value = "STD-AON2"; ws.Cell(3, 3).Value = "Ece B";
        ws.Cell(3, 4).Value = 30; ws.Cell(3, 5).Value = 0; ws.Cell(3, 6).Value = 0; // INVALID (30 != 40)

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "invalid.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>());

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            commitHandler.HandleAsync(exam.Id, ImportType.AggregateResults, "dummyhash", file));

        // Assert 0 database writes
        using var checkDb = Db();
        var attemptsCount = await checkDb.Set<StudentExamAttempt>().CountAsync(a => a.ExamId == exam.Id);
        var subjectResultsCount = await checkDb.Set<StudentExamSubjectResult>().CountAsync(s => s.InstitutionId == inst.Id);
        var ledgerCount = await checkDb.Set<ExamResultImport>().CountAsync(l => l.ExamId == exam.Id);

        Assert.Equal(0, attemptsCount);
        Assert.Equal(0, subjectResultsCount);
        Assert.Equal(0, ledgerCount);
    }

    [Fact]
    public async Task CommitResultImport_StudentIdAndNumberConflict_EmitsConflictIssue()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Conflict" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-C1", FirstName = "Ali", LastName = "X" };
        var s2 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-C2", FirstName = "Veli", LastName = "Y" };
        db.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Conflict Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 20, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        // Row 1: StudentId = s1.Id, StudentNumber = STD-C2 (s2's number) -> CONFLICT!
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Correct"; ws.Cell(1, 5).Value = $"SEC_{section.Id}_Wrong"; ws.Cell(1, 6).Value = $"SEC_{section.Id}_Blank";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-C2"; ws.Cell(2, 3).Value = "Conflict Name";
        ws.Cell(2, 4).Value = 15; ws.Cell(2, 5).Value = 5; ws.Cell(2, 6).Value = 0;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "conflict.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.AggregateResults, file);

        Assert.False(preview.CanImport);
        var conflictIssue = preview.Issues.FirstOrDefault(i => i.Code == ImportIssueCodes.StudentIdentifierConflict);
        Assert.NotNull(conflictIssue);
    }

    [Fact]
    public async Task CommitResultImport_Retake_IncrementsAttemptNumber()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Retake" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var s1 = new Student { InstitutionId = inst.Id, StudentNumber = "STD-RET", FirstName = "Selin", LastName = "M" };
        db.Add(s1);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Retake Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 20, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        // Existing attempt #1
        var existingAttempt = new StudentExamAttempt
        {
            Id = Guid.NewGuid(),
            InstitutionId = inst.Id,
            ExamId = exam.Id,
            StudentId = s1.Id,
            AttemptNumber = 1,
            TakenAt = DateTimeOffset.UtcNow.AddDays(-1),
            Status = AttemptStatus.Finalized,
            FinalizedAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsRawAvailable = false,
            TotalCorrect = 10, TotalWrong = 10, TotalBlank = 0, TotalNet = 7.5m
        };
        db.Add(existingAttempt);
        await db.SaveChangesAsync();

        // Workbook for import
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId"; ws.Cell(1, 2).Value = "StudentNumber"; ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Correct"; ws.Cell(1, 5).Value = $"SEC_{section.Id}_Wrong"; ws.Cell(1, 6).Value = $"SEC_{section.Id}_Blank";

        ws.Cell(2, 1).Value = s1.Id.ToString(); ws.Cell(2, 2).Value = "STD-RET"; ws.Cell(2, 3).Value = "Selin M";
        ws.Cell(2, 4).Value = 18; ws.Cell(2, 5).Value = 2; ws.Cell(2, 6).Value = 0;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var bytes = ms.ToArray();

        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "retake.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);
        var previewEngine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);

        var preview = await previewEngine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.AggregateResults, file);

        var resourceAccess = Substitute.For<IExamResourceAccess>();
        resourceAccess.ValidateExamAccessAsync(exam.Id, Arg.Any<CancellationToken>()).Returns(exam);

        var commitHandler = new CommitResultImportHandler(db, new TestCurrentUser(Guid.NewGuid()), resourceAccess, previewEngine, scoringService, new RawExamAttemptFinalizer(), Substitute.For<IAuditService>());

        await commitHandler.HandleAsync(exam.Id, ImportType.AggregateResults, preview.FileHash, file);

        using var checkDb = Db();
        var attempts = await checkDb.Set<StudentExamAttempt>()
            .Where(a => a.ExamId == exam.Id && a.StudentId == s1.Id)
            .OrderBy(a => a.AttemptNumber)
            .ToListAsync();

        Assert.Equal(2, attempts.Count);
        Assert.Equal(1, attempts[0].AttemptNumber);
        Assert.Equal(2, attempts[1].AttemptNumber);
    }

    [Fact]
    public async Task UploadHardening_MssqlEndToEnd_EnforcesMultipartLimits_And_PreservesMaterialUploadDownloadAndAuditJson()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst Upload Hardening" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var teacherUserId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();

        var teacherAppUser = new ApplicationUser
        {
            Id = teacherUserId,
            UserName = $"teacher.{teacherUserId:N}@test.local",
            NormalizedUserName = $"TEACHER.{teacherUserId:N}@TEST.LOCAL",
            Email = $"teacher.{teacherUserId:N}@test.local",
            NormalizedEmail = $"TEACHER.{teacherUserId:N}@TEST.LOCAL",
            InstitutionId = inst.Id,
            FirstName = "Kemal",
            LastName = "Hoca",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        var studentAppUser = new ApplicationUser
        {
            Id = studentUserId,
            UserName = $"student.{studentUserId:N}@test.local",
            NormalizedUserName = $"STUDENT.{studentUserId:N}@TEST.LOCAL",
            Email = $"student.{studentUserId:N}@test.local",
            NormalizedEmail = $"STUDENT.{studentUserId:N}@TEST.LOCAL",
            InstitutionId = inst.Id,
            FirstName = "Elif",
            LastName = "Demir",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        db.AddRange(teacherAppUser, studentAppUser);
        await db.SaveChangesAsync();

        var teacher = new Teacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = inst.Id,
            UserId = teacherUserId,
            FirstName = "Kemal",
            LastName = "Hoca"
        };
        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            InstitutionId = inst.Id,
            Name = "12-MF",
            GradeLevel = 12,
            AcademicYear = "2026-2027"
        };
        var classTeacher = new ClassroomTeacher
        {
            Id = Guid.NewGuid(),
            InstitutionId = inst.Id,
            TeacherId = teacher.Id,
            ClassroomId = classroom.Id
        };
        var student = new Student
        {
            Id = Guid.NewGuid(),
            InstitutionId = inst.Id,
            UserId = studentUserId,
            StudentNumber = "STD-UPL-1",
            FirstName = "Elif",
            LastName = "Demir"
        };
        var classStudent = new ClassroomStudent
        {
            Id = Guid.NewGuid(),
            InstitutionId = inst.Id,
            ClassroomId = classroom.Id,
            StudentId = student.Id
        };
        db.AddRange(teacher, classroom, classTeacher, student, classStudent);
        await db.SaveChangesAsync();

        var tempStorage = Path.Combine(Path.GetTempPath(), $"egitim_mat_mssql_{Guid.NewGuid():N}");
        try
        {
            var storageService = new LocalFileStorageService(tempStorage);
            var auditService = new AuditService(db);

            var teacherUser = Substitute.For<ICurrentUser>();
            teacherUser.IsAuthenticated.Returns(true);
            teacherUser.UserId.Returns(teacherUserId);
            teacherUser.GetInstitutionIdAsync().Returns(inst.Id);
            teacherUser.Roles.Returns([Roles.Teacher]);

            var teacherMaterialsHandler = new MaterialsHandler(db, teacherUser, storageService, auditService);

            var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF\n");
            var pdfFormFile = new FormFile(new MemoryStream(pdfBytes), 0, pdfBytes.Length, "File", @"..\..\uploads\analitik_""ozel"".pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var specialTitle = "Analitik \"Özel\" Fasikül \\ Bölüm 2";
            var createdMaterial = await teacherMaterialsHandler.CreateMaterialAsync(new CreateMaterialRequest(
                specialTitle,
                "Matematik",
                "Analitik Geometri",
                "12. Sınıf",
                "fasikul",
                "MSSQL entegrasyon testi",
                12,
                30,
                45,
                pdfFormFile,
                [classroom.Id]
            ));

            Assert.Equal("analitik_\"ozel\".pdf", createdMaterial.OriginalFileName);
            Assert.Equal(1, createdMaterial.AssignedClassesCount);

            // Verify Student download against real MSSQL + LocalFileStorageService
            var studentUser = Substitute.For<ICurrentUser>();
            studentUser.IsAuthenticated.Returns(true);
            studentUser.UserId.Returns(studentUserId);
            studentUser.GetInstitutionIdAsync().Returns(inst.Id);
            studentUser.Roles.Returns([Roles.Student]);

            var studentMaterialsHandler = new MaterialsHandler(db, studentUser, storageService, auditService);
            var studentList = await studentMaterialsHandler.GetStudentMaterialsAsync();
            Assert.Single(studentList);
            Assert.Equal(createdMaterial.Id, studentList[0].Id);

            var (downloadStream, contentType, downloadFileName) = await studentMaterialsHandler.DownloadMaterialAsync(createdMaterial.Id);
            await using (downloadStream)
            {
                using var downloadedMs = new MemoryStream();
                await downloadStream.CopyToAsync(downloadedMs);
                Assert.Equal(pdfBytes, downloadedMs.ToArray());
            }
            Assert.Equal("application/pdf", contentType);
            Assert.Equal("analitik_\"ozel\".pdf", downloadFileName);

            // Verify persisted AuditLog in MSSQL has valid JSON
            using var checkDb = Db();
            var persistedAudit = await checkDb.Set<AuditLog>()
                .SingleAsync(a => a.Action == "CreateMaterial" && a.EntityId == createdMaterial.Id.ToString());
            Assert.NotNull(persistedAudit.MetadataJson);
            using var jsonDoc = JsonDocument.Parse(persistedAudit.MetadataJson!);
            Assert.Equal(specialTitle, jsonDoc.RootElement.GetProperty("title").GetString());
            Assert.Equal("analitik_\"ozel\".pdf", jsonDoc.RootElement.GetProperty("fileName").GetString());
        }
        finally
        {
            if (Directory.Exists(tempStorage)) Directory.Delete(tempStorage, true);
        }

        // Verify HTTP pipeline rejects oversized multipart (> 5 MB) on ExamResultImportsController before handler execution
        using var client = factory.CreateClient();
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IntegrationTestFactory.TestSigningKey));
        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = IntegrationTestFactory.TestIssuer,
            Audience = IntegrationTestFactory.TestAudience,
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, Roles.SuperAdmin)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenHandler.WriteToken(token));

        var oversizedBytes = new byte[ImportFileValidator.MaxFileSizeBytes + 1024];
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent("AggregateResults"), "importType");
        var fileContent = new ByteArrayContent(oversizedBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        multipart.Add(fileContent, "file", "oversized.xlsx");

        var previewResp = await client.PostAsync($"/api/v1/exams/{Guid.NewGuid()}/result-imports/preview", multipart);
        Assert.Equal(HttpStatusCode.BadRequest, previewResp.StatusCode);
    }
}
