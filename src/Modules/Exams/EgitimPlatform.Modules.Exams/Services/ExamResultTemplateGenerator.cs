using ClosedXML.Excel;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Services;

public interface IExamResultTemplateGenerator
{
    Task<byte[]> GenerateAggregateTemplateAsync(Guid examId, Guid institutionId, CancellationToken ct = default);
    Task<byte[]> GenerateQuestionAnswersTemplateAsync(Guid examId, Guid institutionId, CancellationToken ct = default);
}

public class ExamResultTemplateGenerator(
    IApplicationDbContext db,
    IAcademicCatalog academicCatalog) : IExamResultTemplateGenerator
{
    public const int CurrentTemplateVersion = 1;

    public async Task<byte[]> GenerateAggregateTemplateAsync(Guid examId, Guid institutionId, CancellationToken ct = default)
    {
        var exam = await db.Set<Exam>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId && e.InstitutionId == institutionId, ct)
            ?? throw new NotFoundException($"Exam '{examId}' was not found.");

        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == examId && s.InstitutionId == institutionId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        if (sections.Count == 0)
        {
            throw new ValidationException($"Exam '{exam.Title}' has no sections defined.");
        }

        var subjectIds = sections.Select(s => s.SubjectId).Distinct().ToList();
        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);

        using var workbook = new XLWorkbook();

        // 1. Metadata Sheet
        var metaWs = workbook.Worksheets.Add("_Metadata");
        metaWs.Cell(1, 1).Value = "EXAM_ID";
        metaWs.Cell(1, 2).Value = examId.ToString();
        metaWs.Cell(2, 1).Value = "IMPORT_TYPE";
        metaWs.Cell(2, 2).Value = "AggregateResults";
        metaWs.Cell(3, 1).Value = "TEMPLATE_VERSION";
        metaWs.Cell(3, 2).Value = CurrentTemplateVersion;
        metaWs.Hide();

        // 2. Data Sheet
        var worksheet = workbook.Worksheets.Add("Aggregate Import");

        // Row 1: Machine Header Row
        var machineRow = worksheet.Row(1);
        machineRow.Style.Font.Bold = true;
        machineRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
        machineRow.Style.Font.FontColor = XLColor.White;

        worksheet.Cell(1, 1).Value = "StudentId";
        worksheet.Cell(1, 2).Value = "StudentNumber";
        worksheet.Cell(1, 3).Value = "StudentName";
        int colIndex = 4;

        foreach (var section in sections)
        {
            worksheet.Cell(1, colIndex++).Value = $"SEC_{section.Id}_Correct";
            worksheet.Cell(1, colIndex++).Value = $"SEC_{section.Id}_Wrong";
            worksheet.Cell(1, colIndex++).Value = $"SEC_{section.Id}_Blank";
        }

        // Row 2: Friendly Display Header Row
        var displayRow = worksheet.Row(2);
        displayRow.Style.Font.Bold = true;
        displayRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#334155");
        displayRow.Style.Font.FontColor = XLColor.White;

        worksheet.Cell(2, 1).Value = "Öğrenci ID (Sistem)";
        worksheet.Cell(2, 2).Value = "Öğrenci No";
        worksheet.Cell(2, 3).Value = "Öğrenci Adı Soyadı";
        colIndex = 4;

        foreach (var section in sections)
        {
            var name = subjectNames.TryGetValue(section.SubjectId, out var sName) ? sName : $"Subject_{section.SubjectId.ToString()[..4]}";
            worksheet.Cell(2, colIndex++).Value = $"{name} - Doğru";
            worksheet.Cell(2, colIndex++).Value = $"{name} - Yanlış";
            worksheet.Cell(2, colIndex++).Value = $"{name} - Boş";
        }

        // Sample Row (Row 3)
        worksheet.Cell(3, 1).Value = "";
        worksheet.Cell(3, 2).Value = "STD-1001";
        worksheet.Cell(3, 3).Value = "Örnek Öğrenci";
        colIndex = 4;
        foreach (var section in sections)
        {
            worksheet.Cell(3, colIndex++).Value = Math.Max(0, section.QuestionCount - 2);
            worksheet.Cell(3, colIndex++).Value = 2;
            worksheet.Cell(3, colIndex++).Value = 0;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> GenerateQuestionAnswersTemplateAsync(Guid examId, Guid institutionId, CancellationToken ct = default)
    {
        var exam = await db.Set<Exam>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId && e.InstitutionId == institutionId, ct)
            ?? throw new NotFoundException($"Exam '{examId}' was not found.");

        var questions = await db.Set<ExamQuestion>()
            .AsNoTracking()
            .Where(q => q.ExamId == examId && q.InstitutionId == institutionId)
            .OrderBy(q => q.QuestionNumber)
            .ToListAsync(ct);

        if (questions.Count == 0)
        {
            throw new ValidationException($"Exam '{exam.Title}' has no questions linked.");
        }

        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == examId && s.InstitutionId == institutionId)
            .ToDictionaryAsync(s => s.Id, ct);

        var subjectIds = sections.Values.Select(s => s.SubjectId).Distinct().ToList();
        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);

        using var workbook = new XLWorkbook();

        // 1. Metadata Sheet
        var metaWs = workbook.Worksheets.Add("_Metadata");
        metaWs.Cell(1, 1).Value = "EXAM_ID";
        metaWs.Cell(1, 2).Value = examId.ToString();
        metaWs.Cell(2, 1).Value = "IMPORT_TYPE";
        metaWs.Cell(2, 2).Value = "QuestionAnswers";
        metaWs.Cell(3, 1).Value = "TEMPLATE_VERSION";
        metaWs.Cell(3, 2).Value = CurrentTemplateVersion;
        metaWs.Hide();

        // 2. Data Sheet
        var worksheet = workbook.Worksheets.Add("Question Answers Import");

        // Row 1: Machine Header Row
        var machineRow = worksheet.Row(1);
        machineRow.Style.Font.Bold = true;
        machineRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
        machineRow.Style.Font.FontColor = XLColor.White;

        worksheet.Cell(1, 1).Value = "StudentId";
        worksheet.Cell(1, 2).Value = "StudentNumber";
        worksheet.Cell(1, 3).Value = "StudentName";
        int colIndex = 4;

        foreach (var question in questions)
        {
            worksheet.Cell(1, colIndex++).Value = $"EQ_{question.Id}";
        }

        // Row 2: Friendly Display Header Row
        var displayRow = worksheet.Row(2);
        displayRow.Style.Font.Bold = true;
        displayRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#334155");
        displayRow.Style.Font.FontColor = XLColor.White;

        worksheet.Cell(2, 1).Value = "Öğrenci ID (Sistem)";
        worksheet.Cell(2, 2).Value = "Öğrenci No";
        worksheet.Cell(2, 3).Value = "Öğrenci Adı Soyadı";
        colIndex = 4;

        foreach (var question in questions)
        {
            string secSubjectName = "Q";
            if (sections.TryGetValue(question.ExamSectionId, out var sec) && subjectNames.TryGetValue(sec.SubjectId, out var sName))
            {
                secSubjectName = sName;
            }
            worksheet.Cell(2, colIndex++).Value = $"{secSubjectName} - S{question.QuestionNumber}";
        }

        // Sample Row (Row 3)
        worksheet.Cell(3, 1).Value = "";
        worksheet.Cell(3, 2).Value = "STD-1001";
        worksheet.Cell(3, 3).Value = "Örnek Öğrenci";
        colIndex = 4;

        var sampleOptions = new[] { "A", "B", "C", "D", "E" };
        int optIdx = 0;
        foreach (var _ in questions)
        {
            worksheet.Cell(3, colIndex++).Value = sampleOptions[optIdx % sampleOptions.Length];
            optIdx++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
