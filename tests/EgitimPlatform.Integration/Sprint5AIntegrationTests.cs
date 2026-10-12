using ClosedXML.Excel;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Integration.Fixtures;
using EgitimPlatform.Modules.Academic.Entities;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.ExamResultImport;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Institutions.Entities;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EgitimPlatform.Integration;

[Collection("Integration")]
public class Sprint5AIntegrationTests(IntegrationTestFactory factory)
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

    private ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(factory.ConnectionString).Options);

    private static string Code() => "S5A-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task GenerateAggregateTemplate_ReturnsValidWorkbookStreamWithMetadataAndMachineHeaders()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst S5A" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Aggregate Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 40, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var generator = new ExamResultTemplateGenerator(db, new DummyAcademicCatalog());
        var bytes = await generator.GenerateAggregateTemplateAsync(exam.Id, inst.Id);

        Assert.NotEmpty(bytes);

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        
        // Metadata Sheet check
        var metaWs = workbook.Worksheets.FirstOrDefault(w => w.Name == "_Metadata");
        Assert.NotNull(metaWs);
        Assert.Equal(exam.Id.ToString(), metaWs.Cell(1, 2).Value.ToString());
        Assert.Equal("AggregateResults", metaWs.Cell(2, 2).Value.ToString());

        // Machine Header Row check
        var ws = workbook.Worksheets.First(w => w.Name != "_Metadata");
        Assert.Equal("StudentId", ws.Cell(1, 1).Value.ToString());
        Assert.Equal("StudentNumber", ws.Cell(1, 2).Value.ToString());
        Assert.Equal($"SEC_{section.Id}_Correct", ws.Cell(1, 4).Value.ToString());
    }

    [Fact]
    public async Task PreviewResultImport_WritesZeroExamResultRowsToDatabaseAndReturnsFileHash()
    {
        using var db = Db();
        var inst = new Institution { Name = "Inst ZeroWrite" };
        db.Add(inst);
        await db.SaveChangesAsync();

        var student = new Student { InstitutionId = inst.Id, StudentNumber = "STD-55", FirstName = "Ahmet", LastName = "K" };
        db.Add(student);
        await db.SaveChangesAsync();

        var examType = new ExamType { Code = Code(), Name = "TYT" };
        var subject = new Subject { Code = Code(), Name = "Matematik", ExamTypeId = examType.Id };
        db.AddRange(examType, subject);
        await db.SaveChangesAsync();

        var exam = new Exam { InstitutionId = inst.Id, ExamTypeId = examType.Id, Code = Code(), Title = "Preview Exam" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var section = new ExamSection { InstitutionId = inst.Id, ExamId = exam.Id, SubjectId = subject.Id, QuestionCount = 20, DisplayOrder = 1 };
        db.Add(section);
        await db.SaveChangesAsync();

        var attemptsBefore = await db.Set<StudentExamAttempt>().CountAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Aggregate");
        ws.Cell(1, 1).Value = "StudentId";
        ws.Cell(1, 2).Value = "StudentNumber";
        ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Correct";
        ws.Cell(1, 5).Value = $"SEC_{section.Id}_Wrong";
        ws.Cell(1, 6).Value = $"SEC_{section.Id}_Blank";

        ws.Cell(2, 1).Value = student.Id.ToString();
        ws.Cell(2, 2).Value = "STD-55";
        ws.Cell(2, 3).Value = "Ahmet K";
        ws.Cell(2, 4).Value = 15;
        ws.Cell(2, 5).Value = 5;
        ws.Cell(2, 6).Value = 0;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var fileBytes = ms.ToArray();

        var fileHeader = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "file", "preview.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var validator = new ImportFileValidator();
        var scoringService = new ExamScoringService();
        var studentDirectory = new StudentDirectory(db);

        var engine = new ExamResultImportPreviewEngine(db, validator, studentDirectory, new DummyAcademicCatalog(), scoringService);
        var preview = await engine.GeneratePreviewAsync(exam.Id, inst.Id, ImportType.AggregateResults, fileHeader);

        var attemptsAfter = await db.Set<StudentExamAttempt>().CountAsync();

        Assert.True(preview.CanImport);
        Assert.NotEmpty(preview.FileHash);
        Assert.Equal(1, preview.ValidRowsCount);
        Assert.Equal(attemptsBefore, attemptsAfter); // Zero write invariant verified!
    }
}
