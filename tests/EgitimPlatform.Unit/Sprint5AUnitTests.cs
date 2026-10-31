using System.Security.Cryptography;
using ClosedXML.Excel;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Infrastructure;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.ExamResultImport;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
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

    [Fact]
    public async Task GeneratePreviewAsync_StreamingSha256_MatchesByteArrayHash_WithChunkedNonSeekableStream()
    {
        using var db = CreateInMemoryDbContext();
        var exam = new Exam { Id = _examId, InstitutionId = _institutionId, Title = "TYT Streaming", Code = "TYT-STR", WrongAnswerPenalty = 4m };
        var section = new ExamSection { Id = Guid.NewGuid(), ExamId = _examId, InstitutionId = _institutionId, SubjectId = _subjectId, QuestionCount = 40, DisplayOrder = 1 };
        db.AddRange(exam, section);
        await db.SaveChangesAsync();

        var studentDto = new StudentImportLookupDto(Guid.NewGuid(), _institutionId, "STD-901", "Zeynep", "Kaya");
        _studentDirectory.GetActiveStudentsForImportAsync(_institutionId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentImportLookupDto> { studentDto });
        _academicCatalog.GetSubjectNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { { _subjectId, "Matematik" } });

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        ws.Cell(1, 1).Value = "StudentId";
        ws.Cell(1, 2).Value = "StudentNumber";
        ws.Cell(1, 3).Value = "StudentName";
        ws.Cell(1, 4).Value = $"SEC_{section.Id}_Correct";
        ws.Cell(1, 5).Value = $"SEC_{section.Id}_Wrong";
        ws.Cell(1, 6).Value = $"SEC_{section.Id}_Blank";
        ws.Cell(2, 1).Value = studentDto.Id.ToString();
        ws.Cell(2, 2).Value = "STD-901";
        ws.Cell(2, 3).Value = "Zeynep Kaya";
        ws.Cell(2, 4).Value = 32;
        ws.Cell(2, 5).Value = 4;
        ws.Cell(2, 6).Value = 4;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var rawBytes = ms.ToArray();
        var expectedHexHash = Convert.ToHexString(SHA256.HashData(rawBytes));

        var file = Substitute.For<IFormFile>();
        file.Length.Returns(rawBytes.Length);
        file.FileName.Returns("streaming.xlsx");
        file.ContentType.Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        // First OpenReadStream call is used for streaming SHA-256 (non-seekable, max 512 bytes per read); second is for workbook open
        var callCount = 0;
        file.OpenReadStream().Returns(_ =>
            Interlocked.Increment(ref callCount) == 1
                ? new ChunkedNonSeekableReadStream(rawBytes, maxBytesPerRead: 512)
                : new MemoryStream(rawBytes));

        var engine = new ExamResultImportPreviewEngine(db, _validator, _studentDirectory, _academicCatalog, _scoringService);
        var preview = await engine.GeneratePreviewAsync(_examId, _institutionId, ImportType.AggregateResults, file);

        Assert.True(preview.CanImport);
        Assert.Equal(expectedHexHash, preview.FileHash);
    }

    [Fact]
    public async Task GeneratePreviewAsync_OversizedOrMalformedFile_ThrowsValidationExceptionBeforeProcessing()
    {
        using var db = CreateInMemoryDbContext();
        var exam = new Exam { Id = _examId, InstitutionId = _institutionId, Title = "TYT Limits", Code = "TYT-LIM" };
        db.Add(exam);
        await db.SaveChangesAsync();

        var engine = new ExamResultImportPreviewEngine(db, _validator, _studentDirectory, _academicCatalog, _scoringService);

        // 1. Oversized file (> 5 MB) rejected before OpenReadStream is ever called
        var oversizedFile = Substitute.For<IFormFile>();
        oversizedFile.Length.Returns(ImportFileValidator.MaxFileSizeBytes + 1);
        oversizedFile.FileName.Returns("oversized.xlsx");
        oversizedFile.ContentType.Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var sizeEx = await Assert.ThrowsAsync<ValidationException>(() =>
            engine.GeneratePreviewAsync(_examId, _institutionId, ImportType.AggregateResults, oversizedFile));
        Assert.Contains("maximum allowed size of 5 MB", sizeEx.Message);
        oversizedFile.DidNotReceive().OpenReadStream();

        // 2. Malformed/corrupt .xlsx bytes rejected with ValidationException
        var corruptBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0xFF, 0x00, 0x11, 0x22, 0x33 };
        var malformedFile = Substitute.For<IFormFile>();
        malformedFile.Length.Returns(corruptBytes.Length);
        malformedFile.FileName.Returns("corrupt.xlsx");
        malformedFile.ContentType.Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        malformedFile.OpenReadStream().Returns(_ => new MemoryStream(corruptBytes));

        var corruptEx = await Assert.ThrowsAsync<ValidationException>(() =>
            engine.GeneratePreviewAsync(_examId, _institutionId, ImportType.AggregateResults, malformedFile));
        Assert.Contains("Failed to read Excel workbook", corruptEx.Message);
    }

    private sealed class ChunkedNonSeekableReadStream(byte[] data, int maxBytesPerRead) : Stream
    {
        private int _position;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= data.Length) return 0;
            var toCopy = Math.Min(Math.Min(count, maxBytesPerRead), data.Length - _position);
            Buffer.BlockCopy(data, _position, buffer, offset, toCopy);
            _position += toCopy;
            return toCopy;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_position >= data.Length) return ValueTask.FromResult(0);
            var toCopy = Math.Min(Math.Min(buffer.Length, maxBytesPerRead), data.Length - _position);
            data.AsMemory(_position, toCopy).CopyTo(buffer);
            _position += toCopy;
            return ValueTask.FromResult(toCopy);
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
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
