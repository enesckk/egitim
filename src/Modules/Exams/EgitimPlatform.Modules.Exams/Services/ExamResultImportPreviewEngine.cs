using System.Security.Cryptography;
using ClosedXML.Excel;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.ExamResultImport;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Services;

public interface IExamResultImportPreviewEngine
{
    Task<ExamResultImportPreviewDto> GeneratePreviewAsync(
        Guid examId,
        Guid institutionId,
        ImportType importType,
        IFormFile file,
        CancellationToken ct = default);
}

public class ExamResultImportPreviewEngine(
    IApplicationDbContext db,
    IImportFileValidator fileValidator,
    IStudentDirectory studentDirectory,
    IAcademicCatalog academicCatalog,
    IExamScoringService scoringService) : IExamResultImportPreviewEngine
{
    private const int MaxAllowedRows = 2000;

    public async Task<ExamResultImportPreviewDto> GeneratePreviewAsync(
        Guid examId,
        Guid institutionId,
        ImportType importType,
        IFormFile file,
        CancellationToken ct = default)
    {
        var exam = await db.Set<Exam>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId && e.InstitutionId == institutionId, ct)
            ?? throw new NotFoundException($"Exam '{examId}' was not found.");

        // Compute File Hash (SHA-256)
        using var stream = file.OpenReadStream();
        var fileBytes = new byte[file.Length];
        int bytesRead = 0;
        while (bytesRead < file.Length)
        {
            int read = await stream.ReadAsync(fileBytes.AsMemory(bytesRead, (int)file.Length - bytesRead), ct);
            if (read == 0) break;
            bytesRead += read;
        }
        var fileHash = Convert.ToHexString(SHA256.HashData(fileBytes));

        using var workbook = fileValidator.OpenWorkbook(file);
        var issues = new List<ImportIssueDto>();

        // Metadata Sheet Check
        int templateVersion = 1;
        var metaWs = workbook.Worksheets.FirstOrDefault(w => string.Equals(w.Name, "_Metadata", StringComparison.OrdinalIgnoreCase));
        if (metaWs != null)
        {
            var metaExamIdStr = metaWs.Cell(1, 2).Value.ToString().Trim();
            if (Guid.TryParse(metaExamIdStr, out var metaExamId) && metaExamId != examId)
            {
                issues.Add(new ImportIssueDto(1, "Metadata", ImportIssueCodes.ExamTemplateMismatch, ImportIssueSeverity.Error.ToString(), $"Template was generated for Exam '{metaExamId}' but target exam is '{examId}'."));
            }

            var metaImportTypeStr = metaWs.Cell(2, 2).Value.ToString().Trim();
            if (!string.IsNullOrEmpty(metaImportTypeStr) && !string.Equals(metaImportTypeStr, importType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ImportIssueDto(1, "Metadata", ImportIssueCodes.InvalidTemplate, ImportIssueSeverity.Error.ToString(), $"Template import type '{metaImportTypeStr}' does not match request import type '{importType}'."));
            }

            var metaVersionStr = metaWs.Cell(3, 2).Value.ToString().Trim();
            if (int.TryParse(metaVersionStr, out var ver))
            {
                templateVersion = ver;
                if (ver > 1)
                {
                    issues.Add(new ImportIssueDto(1, "Metadata", ImportIssueCodes.UnsupportedTemplateVersion, ImportIssueSeverity.Error.ToString(), $"Unsupported template version '{ver}'. Maximum supported version is 1."));
                }
            }
        }

        var worksheet = workbook.Worksheets.FirstOrDefault(w => !string.Equals(w.Name, "_Metadata", StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException("Workbook contains no visible data worksheets.");

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        // Detect if Row 2 is Display Header (Row 1 has machine keys, Row 2 has friendly labels)
        int headerRowIndex = 1;
        int firstDataRowIndex = 2;

        var r1Col4 = worksheet.Row(1).Cell(4).Value.ToString().Trim();
        var r2Col4 = worksheet.Row(2).Cell(4).Value.ToString().Trim();

        if (r1Col4.StartsWith("SEC_") || r1Col4.StartsWith("EQ_") || r1Col4.Equals("StudentId", StringComparison.OrdinalIgnoreCase))
        {
            if (r2Col4.Contains("-") || r2Col4.Contains("Öğrenci") || r2Col4.Contains("Doğru"))
            {
                firstDataRowIndex = 3;
            }
        }

        if (lastRowNumber < firstDataRowIndex)
        {
            issues.Add(new ImportIssueDto(1, "File", ImportIssueCodes.FileEmpty, ImportIssueSeverity.Error.ToString(), "Workbook contains no student data rows."));
            return CreateEmptyPreview(fileHash, examId, importType, templateVersion, issues);
        }

        var totalRowsCount = lastRowNumber - firstDataRowIndex + 1;
        if (totalRowsCount > MaxAllowedRows)
        {
            throw new ValidationException($"Import contains {totalRowsCount} rows, exceeding maximum limit of {MaxAllowedRows} rows.");
        }

        // Batch pre-load active students for current institution
        var students = await studentDirectory.GetActiveStudentsForImportAsync(institutionId, ct);

        var studentById = students.ToDictionary(s => s.Id);
        var studentNumberGroups = students
            .Where(s => !string.IsNullOrWhiteSpace(s.StudentNumber))
            .GroupBy(s => s.StudentNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var studentFullNameMap = students
            .GroupBy(s => $"{s.FirstName} {s.LastName}".Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        if (importType == ImportType.AggregateResults)
        {
            return await ProcessAggregatePreviewAsync(
                worksheet, fileHash, exam, institutionId, templateVersion, studentById, studentNumberGroups, studentFullNameMap, headerRowIndex, firstDataRowIndex, totalRowsCount, issues, ct);
        }
        else
        {
            return await ProcessQuestionAnswersPreviewAsync(
                worksheet, fileHash, exam, institutionId, templateVersion, studentById, studentNumberGroups, studentFullNameMap, headerRowIndex, firstDataRowIndex, totalRowsCount, issues, ct);
        }
    }

    private async Task<ExamResultImportPreviewDto> ProcessAggregatePreviewAsync(
        IXLWorksheet worksheet,
        string fileHash,
        Exam exam,
        Guid institutionId,
        int templateVersion,
        Dictionary<Guid, StudentImportLookupDto> studentById,
        Dictionary<string, List<StudentImportLookupDto>> studentNumberGroups,
        Dictionary<string, List<StudentImportLookupDto>> studentFullNameMap,
        int headerRowIndex,
        int firstDataRowIndex,
        int totalRowsCount,
        List<ImportIssueDto> issues,
        CancellationToken ct)
    {
        var rowsPreview = new List<ImportRowPreviewDto>();

        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == exam.Id && s.InstitutionId == institutionId)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        if (sections.Count == 0)
        {
            issues.Add(new ImportIssueDto(1, "Exam", ImportIssueCodes.SectionNotFound, ImportIssueSeverity.Error.ToString(), "Exam has no sections defined."));
            return CreateEmptyPreview(fileHash, exam.Id, ImportType.AggregateResults, templateVersion, issues);
        }

        var sectionById = sections.ToDictionary(s => s.Id);
        var subjectIds = sections.Select(s => s.SubjectId).Distinct().ToList();
        var subjectNames = await academicCatalog.GetSubjectNamesAsync(subjectIds, ct);

        // Map Columns
        var headerRow = worksheet.Row(headerRowIndex);
        var lastColNumber = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        int studentIdCol = 1;
        int studentNumCol = 2;

        var sectionColMap = new Dictionary<Guid, (int CorrectCol, int WrongCol, int BlankCol)>();

        for (int col = 1; col <= lastColNumber; col++)
        {
            var hVal = headerRow.Cell(col).Value.ToString().Trim();
            if (string.Equals(hVal, "StudentId", StringComparison.OrdinalIgnoreCase))
            {
                studentIdCol = col;
            }
            else if (string.Equals(hVal, "StudentNumber", StringComparison.OrdinalIgnoreCase) || string.Equals(hVal, "OgrenciNo", StringComparison.OrdinalIgnoreCase) || string.Equals(hVal, "StudentIdentifier", StringComparison.OrdinalIgnoreCase))
            {
                studentNumCol = col;
            }
            else if (hVal.StartsWith("SEC_", StringComparison.OrdinalIgnoreCase))
            {
                // Format: SEC_{SectionId}_{Type}
                var parts = hVal.Split('_');
                if (parts.Length >= 3 && Guid.TryParse(parts[1], out var secGuid))
                {
                    if (!sectionById.ContainsKey(secGuid))
                    {
                        issues.Add(new ImportIssueDto(1, hVal, ImportIssueCodes.SectionNotFound, ImportIssueSeverity.Error.ToString(), $"Section ID '{secGuid}' in column header does not belong to Exam '{exam.Id}'."));
                    }
                    else
                    {
                        var metricType = parts[2].ToLowerInvariant();
                        if (!sectionColMap.TryGetValue(secGuid, out var triplet))
                        {
                            triplet = (-1, -1, -1);
                        }

                        if (metricType.StartsWith("correct") || metricType.StartsWith("dogru"))
                            triplet.CorrectCol = col;
                        else if (metricType.StartsWith("wrong") || metricType.StartsWith("yanlis"))
                            triplet.WrongCol = col;
                        else if (metricType.StartsWith("blank") || metricType.StartsWith("bos"))
                            triplet.BlankCol = col;

                        sectionColMap[secGuid] = triplet;
                    }
                }
            }
        }

        // Positional fallback for legacy files
        if (sectionColMap.Count == 0 && lastColNumber >= 3 + (sections.Count * 3))
        {
            int cIndex = 4;
            foreach (var sec in sections)
            {
                sectionColMap[sec.Id] = (cIndex, cIndex + 1, cIndex + 2);
                cIndex += 3;
            }
        }

        var seenStudentIds = new HashSet<Guid>();

        for (int r = firstDataRowIndex; r < firstDataRowIndex + totalRowsCount; r++)
        {
            var row = worksheet.Row(r);
            var studentIdVal = row.Cell(studentIdCol).Value.ToString().Trim();
            var studentNumVal = row.Cell(studentNumCol).Value.ToString().Trim();
            var studentIdentifier = !string.IsNullOrEmpty(studentNumVal) ? studentNumVal : studentIdVal;

            bool isRowValid = true;
            MatchedStudentDto? matchedStudent = null;

            matchedStudent = MatchStudentAuthoritatively(studentIdVal, studentNumVal, studentById, studentNumberGroups, studentFullNameMap, issues, r);
            if (matchedStudent == null)
            {
                isRowValid = false;
            }
            else if (!seenStudentIds.Add(matchedStudent.Id))
            {
                issues.Add(new ImportIssueDto(r, "StudentIdentifier", ImportIssueCodes.StudentDuplicate, ImportIssueSeverity.Error.ToString(), $"Duplicate student '{matchedStudent.FullName}' in import file."));
                isRowValid = false;
            }

            var parsedSections = new List<ParsedAggregateSectionDto>();
            decimal totalRowNet = 0m;

            foreach (var sec in sections)
            {
                var sName = subjectNames.TryGetValue(sec.SubjectId, out var name) ? name : "Subject";
                int correct = 0, wrong = 0, blank = 0;

                if (sectionColMap.TryGetValue(sec.Id, out var cols) && cols.CorrectCol != -1 && cols.WrongCol != -1 && cols.BlankCol != -1)
                {
                    var cValStr = row.Cell(cols.CorrectCol).Value.ToString().Trim();
                    var wValStr = row.Cell(cols.WrongCol).Value.ToString().Trim();
                    var bValStr = row.Cell(cols.BlankCol).Value.ToString().Trim();

                    if (!int.TryParse(cValStr, out correct) || correct < 0)
                    {
                        issues.Add(new ImportIssueDto(r, $"{sName}_Correct", ImportIssueCodes.InvalidCorrectCount, ImportIssueSeverity.Error.ToString(), $"Invalid correct count '{cValStr}' for section '{sName}'."));
                        isRowValid = false;
                    }
                    if (!int.TryParse(wValStr, out wrong) || wrong < 0)
                    {
                        issues.Add(new ImportIssueDto(r, $"{sName}_Wrong", ImportIssueCodes.InvalidWrongCount, ImportIssueSeverity.Error.ToString(), $"Invalid wrong count '{wValStr}' for section '{sName}'."));
                        isRowValid = false;
                    }
                    if (!int.TryParse(bValStr, out blank) || blank < 0)
                    {
                        issues.Add(new ImportIssueDto(r, $"{sName}_Blank", ImportIssueCodes.InvalidBlankCount, ImportIssueSeverity.Error.ToString(), $"Invalid blank count '{bValStr}' for section '{sName}'."));
                        isRowValid = false;
                    }

                    if (isRowValid && (correct + wrong + blank != sec.QuestionCount))
                    {
                        issues.Add(new ImportIssueDto(r, sName, ImportIssueCodes.QuestionCountMismatch, ImportIssueSeverity.Error.ToString(), $"Total questions ({correct + wrong + blank}) for '{sName}' does not equal section count ({sec.QuestionCount})."));
                        isRowValid = false;
                    }
                }
                else
                {
                    issues.Add(new ImportIssueDto(r, sName, ImportIssueCodes.SectionNotFound, ImportIssueSeverity.Error.ToString(), $"Column mapping for section '{sName}' ({sec.Id}) is missing or incomplete."));
                    isRowValid = false;
                }

                var penalty = sec.WrongAnswerPenalty ?? exam.WrongAnswerPenalty;
                var secNet = scoringService.CalculateNet(correct, wrong, penalty);
                totalRowNet += secNet;

                parsedSections.Add(new ParsedAggregateSectionDto(sec.Id, sec.SubjectId, sName, correct, wrong, blank, secNet));
            }

            rowsPreview.Add(new ImportRowPreviewDto(
                r - firstDataRowIndex + 1,
                studentIdentifier,
                matchedStudent,
                isRowValid,
                isRowValid ? totalRowNet : null,
                parsedSections,
                []));
        }

        int invalidRowsCount = rowsPreview.Count(r => !r.IsValid);
        int validRowsCount = rowsPreview.Count(r => r.IsValid);
        int warningCount = issues.Count(i => i.Severity == ImportIssueSeverity.Warning.ToString());
        bool canImport = invalidRowsCount == 0 && issues.All(i => i.Severity != ImportIssueSeverity.Error.ToString());

        return new ExamResultImportPreviewDto(
            fileHash,
            exam.Id,
            ImportType.AggregateResults,
            templateVersion,
            totalRowsCount,
            validRowsCount,
            invalidRowsCount,
            warningCount,
            canImport,
            issues,
            rowsPreview);
    }

    private async Task<ExamResultImportPreviewDto> ProcessQuestionAnswersPreviewAsync(
        IXLWorksheet worksheet,
        string fileHash,
        Exam exam,
        Guid institutionId,
        int templateVersion,
        Dictionary<Guid, StudentImportLookupDto> studentById,
        Dictionary<string, List<StudentImportLookupDto>> studentNumberGroups,
        Dictionary<string, List<StudentImportLookupDto>> studentFullNameMap,
        int headerRowIndex,
        int firstDataRowIndex,
        int totalRowsCount,
        List<ImportIssueDto> issues,
        CancellationToken ct)
    {
        var rowsPreview = new List<ImportRowPreviewDto>();

        var questions = await db.Set<ExamQuestion>()
            .AsNoTracking()
            .Where(q => q.ExamId == exam.Id && q.InstitutionId == institutionId)
            .OrderBy(q => q.QuestionNumber)
            .ToListAsync(ct);

        if (questions.Count == 0)
        {
            issues.Add(new ImportIssueDto(1, "Exam", ImportIssueCodes.QuestionNumberNotFound, ImportIssueSeverity.Error.ToString(), "Exam has no linked questions."));
            return CreateEmptyPreview(fileHash, exam.Id, ImportType.QuestionAnswers, templateVersion, issues);
        }

        var sections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == exam.Id)
            .ToDictionaryAsync(s => s.Id, ct);

        var questionById = questions.ToDictionary(q => q.Id);

        var headerRow = worksheet.Row(headerRowIndex);
        var lastColNumber = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        int studentIdCol = 1;
        int studentNumCol = 2;

        var questionColMap = new Dictionary<Guid, int>(); // ExamQuestionId -> ColIndex
        var seenQGuids = new HashSet<Guid>();

        for (int col = 1; col <= lastColNumber; col++)
        {
            var hVal = headerRow.Cell(col).Value.ToString().Trim();
            if (string.Equals(hVal, "StudentId", StringComparison.OrdinalIgnoreCase))
            {
                studentIdCol = col;
            }
            else if (string.Equals(hVal, "StudentNumber", StringComparison.OrdinalIgnoreCase) || string.Equals(hVal, "OgrenciNo", StringComparison.OrdinalIgnoreCase) || string.Equals(hVal, "StudentIdentifier", StringComparison.OrdinalIgnoreCase))
            {
                studentNumCol = col;
            }
            else if (hVal.StartsWith("EQ_", StringComparison.OrdinalIgnoreCase))
            {
                var qGuidStr = hVal[3..];
                if (Guid.TryParse(qGuidStr, out var qGuid))
                {
                    if (!questionById.ContainsKey(qGuid))
                    {
                        issues.Add(new ImportIssueDto(1, hVal, ImportIssueCodes.QuestionNumberNotFound, ImportIssueSeverity.Error.ToString(), $"ExamQuestion '{qGuid}' in template column does not belong to Exam '{exam.Id}'."));
                    }
                    else if (!seenQGuids.Add(qGuid))
                    {
                        issues.Add(new ImportIssueDto(1, hVal, ImportIssueCodes.DuplicateQuestionColumn, ImportIssueSeverity.Error.ToString(), $"Duplicate ExamQuestion column '{hVal}' in template."));
                    }
                    else
                    {
                        questionColMap[qGuid] = col;
                    }
                }
            }
        }

        // Positional fallback for legacy single-header files
        if (questionColMap.Count == 0 && lastColNumber >= 3 + questions.Count)
        {
            int cIndex = 4;
            foreach (var q in questions)
            {
                questionColMap[q.Id] = cIndex++;
            }
        }

        var seenStudentIds = new HashSet<Guid>();
        var allowedOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "", "A", "B", "C", "D", "E" };

        for (int r = firstDataRowIndex; r < firstDataRowIndex + totalRowsCount; r++)
        {
            var row = worksheet.Row(r);
            var studentIdVal = row.Cell(studentIdCol).Value.ToString().Trim();
            var studentNumVal = row.Cell(studentNumCol).Value.ToString().Trim();
            var studentIdentifier = !string.IsNullOrEmpty(studentNumVal) ? studentNumVal : studentIdVal;

            bool isRowValid = true;
            MatchedStudentDto? matchedStudent = null;

            matchedStudent = MatchStudentAuthoritatively(studentIdVal, studentNumVal, studentById, studentNumberGroups, studentFullNameMap, issues, r);
            if (matchedStudent == null)
            {
                isRowValid = false;
            }
            else if (!seenStudentIds.Add(matchedStudent.Id))
            {
                issues.Add(new ImportIssueDto(r, "StudentIdentifier", ImportIssueCodes.StudentDuplicate, ImportIssueSeverity.Error.ToString(), $"Duplicate student '{matchedStudent.FullName}' in import file."));
                isRowValid = false;
            }

            var parsedAnswers = new List<ParsedQuestionAnswerDto>();
            var sectionCounts = new Dictionary<Guid, (int Correct, int Wrong)>();

            foreach (var q in questions)
            {
                string selectedOption = string.Empty;

                if (questionColMap.TryGetValue(q.Id, out var colIndex))
                {
                    selectedOption = row.Cell(colIndex).Value.ToString().Trim().ToUpperInvariant();
                    if (!allowedOptions.Contains(selectedOption))
                    {
                        issues.Add(new ImportIssueDto(r, $"Q{q.QuestionNumber}", ImportIssueCodes.InvalidOption, ImportIssueSeverity.Error.ToString(), $"Invalid answer choice '{selectedOption}' for Q{q.QuestionNumber}. Allowed options are A-E or blank."));
                        isRowValid = false;
                    }
                }
                else
                {
                    issues.Add(new ImportIssueDto(r, $"Q{q.QuestionNumber}", ImportIssueCodes.QuestionNumberNotFound, ImportIssueSeverity.Error.ToString(), $"Column mapping for Q{q.QuestionNumber} ({q.Id}) could not be resolved."));
                    isRowValid = false;
                }

                bool isBlank = string.IsNullOrEmpty(selectedOption);
                bool isCorrect = !isBlank && string.Equals(selectedOption, q.CorrectOption, StringComparison.OrdinalIgnoreCase);

                if (!sectionCounts.TryGetValue(q.ExamSectionId, out var counts))
                {
                    counts = (0, 0);
                }

                if (isCorrect) counts.Correct++;
                else if (!isBlank) counts.Wrong++;

                sectionCounts[q.ExamSectionId] = counts;

                parsedAnswers.Add(new ParsedQuestionAnswerDto(q.Id, q.QuestionNumber, selectedOption, isCorrect, isBlank));
            }

            decimal netScore = 0m;
            foreach (var (secId, counts) in sectionCounts)
            {
                var secPenalty = sections.TryGetValue(secId, out var sec) && sec.WrongAnswerPenalty.HasValue
                    ? sec.WrongAnswerPenalty.Value
                    : exam.WrongAnswerPenalty;

                netScore += scoringService.CalculateNet(counts.Correct, counts.Wrong, secPenalty);
            }

            rowsPreview.Add(new ImportRowPreviewDto(
                r - firstDataRowIndex + 1,
                studentIdentifier,
                matchedStudent,
                isRowValid,
                isRowValid ? netScore : null,
                [],
                parsedAnswers));
        }

        int invalidRowsCount = rowsPreview.Count(r => !r.IsValid);
        int validRowsCount = rowsPreview.Count(r => r.IsValid);
        int warningCount = issues.Count(i => i.Severity == ImportIssueSeverity.Warning.ToString());
        bool canImport = invalidRowsCount == 0 && issues.All(i => i.Severity != ImportIssueSeverity.Error.ToString());

        return new ExamResultImportPreviewDto(
            fileHash,
            exam.Id,
            ImportType.QuestionAnswers,
            templateVersion,
            totalRowsCount,
            validRowsCount,
            invalidRowsCount,
            warningCount,
            canImport,
            issues,
            rowsPreview);
    }

    private static MatchedStudentDto? MatchStudentAuthoritatively(
        string studentIdVal,
        string studentNumVal,
        Dictionary<Guid, StudentImportLookupDto> studentById,
        Dictionary<string, List<StudentImportLookupDto>> studentNumberGroups,
        Dictionary<string, List<StudentImportLookupDto>> studentFullNameMap,
        List<ImportIssueDto> issues,
        int rowNumber)
    {
        // 1st Preference: System StudentId Guid
        if (Guid.TryParse(studentIdVal, out var parsedGuid))
        {
            if (studentById.TryGetValue(parsedGuid, out var sByGuid))
            {
                if (!string.IsNullOrEmpty(studentNumVal) &&
                    studentNumberGroups.TryGetValue(studentNumVal, out var numMatches) &&
                    numMatches.Count == 1 &&
                    numMatches[0].Id != sByGuid.Id)
                {
                    issues.Add(new ImportIssueDto(rowNumber, "StudentIdentifier", ImportIssueCodes.StudentIdentifierConflict, ImportIssueSeverity.Error.ToString(), $"StudentId '{studentIdVal}' (resolves to '{sByGuid.FirstName} {sByGuid.LastName}') conflicts with StudentNumber '{studentNumVal}' (resolves to '{numMatches[0].FirstName} {numMatches[0].LastName}')."));
                    return null;
                }

                return new MatchedStudentDto(sByGuid.Id, sByGuid.StudentNumber, $"{sByGuid.FirstName} {sByGuid.LastName}".Trim(), "StudentId");
            }
            issues.Add(new ImportIssueDto(rowNumber, "StudentId", ImportIssueCodes.StudentNotFound, ImportIssueSeverity.Error.ToString(), $"StudentId '{studentIdVal}' does not belong to active institution."));
            return null;
        }

        // 2nd Preference: StudentNumber
        var numToMatch = !string.IsNullOrEmpty(studentNumVal) ? studentNumVal : studentIdVal;
        if (!string.IsNullOrEmpty(numToMatch))
        {
            if (studentNumberGroups.TryGetValue(numToMatch, out var matchingStudents))
            {
                if (matchingStudents.Count == 1)
                {
                    var sByNum = matchingStudents[0];
                    return new MatchedStudentDto(sByNum.Id, sByNum.StudentNumber, $"{sByNum.FirstName} {sByNum.LastName}".Trim(), "StudentNumber");
                }
                else
                {
                    issues.Add(new ImportIssueDto(rowNumber, "StudentNumber", ImportIssueCodes.StudentIdentifierAmbiguous, ImportIssueSeverity.Error.ToString(), $"Multiple active students ({matchingStudents.Count}) share StudentNumber '{numToMatch}'."));
                    return null;
                }
            }
        }

        // Check if Full Name matches (STRICT POLICY: Full name match is NOT authoritative -> ERROR)
        var nameToMatch = numToMatch;
        if (!string.IsNullOrEmpty(nameToMatch) && studentFullNameMap.TryGetValue(nameToMatch, out var nameMatches))
        {
            var matchNames = string.Join(", ", nameMatches.Select(m => $"{m.FirstName} {m.LastName} ({m.StudentNumber})"));
            issues.Add(new ImportIssueDto(rowNumber, "StudentIdentifier", ImportIssueCodes.StudentMatchSuggestion, ImportIssueSeverity.Error.ToString(), $"Identifier '{nameToMatch}' matched by student full name. Full name is not an authoritative identifier. Please provide a unique StudentNumber or StudentId. Candidates: {matchNames}."));
            return null;
        }

        issues.Add(new ImportIssueDto(rowNumber, "StudentIdentifier", ImportIssueCodes.StudentNotFound, ImportIssueSeverity.Error.ToString(), $"No active student found matching identifier '{numToMatch}' within institution."));
        return null;
    }

    private static ExamResultImportPreviewDto CreateEmptyPreview(string fileHash, Guid examId, ImportType importType, int templateVersion, List<ImportIssueDto> issues)
    {
        int warningCount = issues.Count(i => i.Severity == ImportIssueSeverity.Warning.ToString());
        return new ExamResultImportPreviewDto(
            fileHash,
            examId,
            importType,
            templateVersion,
            0,
            0,
            0,
            warningCount,
            false,
            issues,
            []);
    }
}
