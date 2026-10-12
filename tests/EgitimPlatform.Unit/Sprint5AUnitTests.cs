using ClosedXML.Excel;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.ExamResultImport;
using EgitimPlatform.Modules.Exams.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace EgitimPlatform.Unit;

public class Sprint5AUnitTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private readonly IImportFileValidator _validator = new ImportFileValidator();
    private readonly IStudentDirectory _studentDirectory = Substitute.For<IStudentDirectory>();
    private readonly IAcademicCatalog _academicCatalog = Substitute.For<IAcademicCatalog>();
    private readonly IExamScoringService _scoringService = new ExamScoringService();

    private readonly Guid _institutionId = Guid.NewGuid();
    private readonly Guid _examId = Guid.NewGuid();
    private readonly Guid _subjectId = Guid.NewGuid();

    [Fact]
    public async Task ProcessAggregatePreviewAsync_ValidMachineHeaderFile_ReturnsCanImportTrueAndFileHash()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var exam = new Exam { Id = _examId, InstitutionId = _institutionId, Title = "TYT 1", Code = "TYT-1", WrongAnswerPenalty = 4m };
        var section = new ExamSection { Id = Guid.NewGuid(), ExamId = _examId, InstitutionId = _institutionId, SubjectId = _subjectId, QuestionCount = 40, DisplayOrder = 1 };
        db.AddRange(exam, section);
        await db.SaveChangesAsync();

        var studentDto = new StudentImportLookupDto(Guid.NewGuid(), _institutionId, "STD-101", "Ali", "Yilmaz");

        _studentDirectory.GetActiveStudentsForImportAsync(_institutionId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentImportLookupDto> { studentDto });

        _academicCatalog.GetSubjectNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { { _subjectId, "Matematik" } });

        using var workbook = new XLWorkbook();
        
        // Metadata Sheet
        var metaWs = workbook.Worksheets.Add("_Metadata");
        metaWs.Cell(1, 1).Value = "EXAM_ID"; metaWs.Cell(1, 2).Value = _examId.ToString();
        metaWs.Cell(2, 1).Value = "IMPORT_TYPE"; metaWs.Cell(2, 2).Value = "AggregateResults";
        metaWs.Cell(3, 1).Value = "TEMPLATE_VERSION"; metaWs.Cell(3, 2).Value = "1";

        // Data Sheet
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId";
        ws.Cell(1, 2).Value = "StudentNumber";
        ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Correct";
        ws.Cell(1, 5).Value = $"SEC_{section.Id}_Wrong";
        ws.Cell(1, 6).Value = $"SEC_{section.Id}_Blank";

        ws.Cell(2, 1).Value = "Öğrenci ID";
        ws.Cell(2, 2).Value = "Öğrenci No";
        ws.Cell(2, 3).Value = "Adı Soyadı";
        ws.Cell(2, 4).Value = "Matematik - Doğru";
        ws.Cell(2, 5).Value = "Matematik - Yanlış";
        ws.Cell(2, 6).Value = "Matematik - Boş";

        ws.Cell(3, 1).Value = studentDto.Id.ToString();
        ws.Cell(3, 2).Value = "STD-101";
        ws.Cell(3, 3).Value = "Ali Yilmaz";
        ws.Cell(3, 4).Value = 30;
        ws.Cell(3, 5).Value = 8;
        ws.Cell(3, 6).Value = 2;

        var file = CreateMockFormFile(workbook);
        var engine = new ExamResultImportPreviewEngine(db, _validator, _studentDirectory, _academicCatalog, _scoringService);

        // Act
        var result = await engine.GeneratePreviewAsync(_examId, _institutionId, ImportType.AggregateResults, file);

        // Assert
        Assert.True(result.CanImport);
        Assert.NotEmpty(result.FileHash);
        Assert.Equal(64, result.FileHash.Length); // SHA-256 Hex String length
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.ValidRowsCount);
        Assert.Single(result.Rows);
        Assert.Equal(28m, result.Rows[0].CalculatedTotalNet);
    }

    [Fact]
    public async Task ProcessPreviewAsync_FullNameOnlyMatch_ReturnsStudentMatchSuggestionError()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var exam = new Exam { Id = _examId, InstitutionId = _institutionId, Title = "TYT 1" };
        var section = new ExamSection { Id = Guid.NewGuid(), ExamId = _examId, InstitutionId = _institutionId, SubjectId = _subjectId, QuestionCount = 40 };
        db.AddRange(exam, section);
        await db.SaveChangesAsync();

        var studentDto = new StudentImportLookupDto(Guid.NewGuid(), _institutionId, "STD-555", "Canan", "Kaya");

        _studentDirectory.GetActiveStudentsForImportAsync(_institutionId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentImportLookupDto> { studentDto });

        _academicCatalog.GetSubjectNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { { _subjectId, "Matematik" } });

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId";
        ws.Cell(1, 2).Value = "StudentNumber";
        ws.Cell(1, 3).Value = $"SEC_{section.Id}_Correct";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Wrong";
        ws.Cell(1, 5).Value = $"SEC_{section.Id}_Blank";

        // Upload cell contains only full name "Canan Kaya" in StudentNumber column (no StudentId or StudentNumber match)
        ws.Cell(2, 1).Value = "";
        ws.Cell(2, 2).Value = "Canan Kaya";
        ws.Cell(2, 3).Value = 30;
        ws.Cell(2, 4).Value = 8;
        ws.Cell(2, 5).Value = 2;

        var file = CreateMockFormFile(workbook);
        var engine = new ExamResultImportPreviewEngine(db, _validator, _studentDirectory, _academicCatalog, _scoringService);

        // Act
        var result = await engine.GeneratePreviewAsync(_examId, _institutionId, ImportType.AggregateResults, file);

        // Assert: Full Name match is NOT authoritative -> CanImport = false with STUDENT_MATCH_SUGGESTION
        Assert.False(result.CanImport);
        Assert.Contains(result.Issues, i => i.Code == ImportIssueCodes.StudentMatchSuggestion && i.Severity == ImportIssueSeverity.Error.ToString());
    }

    [Fact]
    public async Task ProcessPreviewAsync_DuplicateStudentNumberInInstitution_ReturnsAmbiguousError()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var exam = new Exam { Id = _examId, InstitutionId = _institutionId, Title = "TYT 1" };
        var section = new ExamSection { Id = Guid.NewGuid(), ExamId = _examId, InstitutionId = _institutionId, SubjectId = _subjectId, QuestionCount = 40 };
        db.AddRange(exam, section);
        await db.SaveChangesAsync();

        // 2 active students in institution share StudentNumber "DUPLICATE-NO"
        var s1 = new StudentImportLookupDto(Guid.NewGuid(), _institutionId, "DUPLICATE-NO", "Mehmet", "1");
        var s2 = new StudentImportLookupDto(Guid.NewGuid(), _institutionId, "DUPLICATE-NO", "Mehmet", "2");

        _studentDirectory.GetActiveStudentsForImportAsync(_institutionId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentImportLookupDto> { s1, s2 });

        _academicCatalog.GetSubjectNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { { _subjectId, "Matematik" } });

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId";
        ws.Cell(1, 2).Value = "StudentNumber";
        ws.Cell(1, 3).Value = $"SEC_{section.Id}_Correct";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Wrong";
        ws.Cell(1, 5).Value = $"SEC_{section.Id}_Blank";

        ws.Cell(2, 1).Value = "";
        ws.Cell(2, 2).Value = "DUPLICATE-NO";
        ws.Cell(2, 3).Value = 30;
        ws.Cell(2, 4).Value = 8;
        ws.Cell(2, 5).Value = 2;

        var file = CreateMockFormFile(workbook);
        var engine = new ExamResultImportPreviewEngine(db, _validator, _studentDirectory, _academicCatalog, _scoringService);

        // Act
        var result = await engine.GeneratePreviewAsync(_examId, _institutionId, ImportType.AggregateResults, file);

        // Assert
        Assert.False(result.CanImport);
        Assert.Contains(result.Issues, i => i.Code == ImportIssueCodes.StudentIdentifierAmbiguous);
    }

    [Fact]
    public async Task ProcessPreviewAsync_ExamMismatchInMetadata_ReturnsMismatchIssue()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var exam = new Exam { Id = _examId, InstitutionId = _institutionId, Title = "TYT 1" };
        var section = new ExamSection { Id = Guid.NewGuid(), ExamId = _examId, InstitutionId = _institutionId, SubjectId = _subjectId, QuestionCount = 40 };
        db.AddRange(exam, section);
        await db.SaveChangesAsync();

        using var workbook = new XLWorkbook();
        var metaWs = workbook.Worksheets.Add("_Metadata");
        metaWs.Cell(1, 1).Value = "EXAM_ID"; metaWs.Cell(1, 2).Value = Guid.NewGuid().ToString(); // Mismatched ExamId

        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId";

        var file = CreateMockFormFile(workbook);
        var engine = new ExamResultImportPreviewEngine(db, _validator, _studentDirectory, _academicCatalog, _scoringService);

        // Act
        var result = await engine.GeneratePreviewAsync(_examId, _institutionId, ImportType.AggregateResults, file);

        // Assert
        Assert.False(result.CanImport);
        Assert.Contains(result.Issues, i => i.Code == ImportIssueCodes.ExamTemplateMismatch);
    }

    private static IFormFile CreateMockFormFile(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        var file = Substitute.For<IFormFile>();
        file.Length.Returns(bytes.Length);
        file.FileName.Returns("import.xlsx");
        file.ContentType.Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        file.OpenReadStream().Returns(_ => new MemoryStream(bytes));
        return file;
    }
}
