namespace EgitimPlatform.Modules.Exams.Features.ExamResultImport;

public enum ImportType
{
    AggregateResults = 1,
    QuestionAnswers = 2
}

public enum ImportIssueSeverity
{
    Error = 1,
    Warning = 2
}

public static class ImportIssueCodes
{
    public const string StudentNotFound = "STUDENT_NOT_FOUND";
    public const string StudentDuplicate = "STUDENT_DUPLICATE";
    public const string StudentIdentifierAmbiguous = "STUDENT_IDENTIFIER_AMBIGUOUS";
    public const string StudentIdentifierConflict = "STUDENT_IDENTIFIER_CONFLICT";
    public const string StudentMatchSuggestion = "STUDENT_MATCH_SUGGESTION";
    public const string DuplicateRow = "DUPLICATE_ROW";
    public const string InvalidCorrectCount = "INVALID_CORRECT_COUNT";
    public const string InvalidWrongCount = "INVALID_WRONG_COUNT";
    public const string InvalidBlankCount = "INVALID_BLANK_COUNT";
    public const string SectionNotFound = "SECTION_NOT_FOUND";
    public const string QuestionNumberNotFound = "QUESTION_NUMBER_NOT_FOUND";
    public const string InvalidOption = "INVALID_OPTION";
    public const string DuplicateQuestionColumn = "DUPLICATE_QUESTION_COLUMN";
    public const string InvalidTemplate = "INVALID_TEMPLATE";
    public const string ExamTemplateMismatch = "EXAM_TEMPLATE_MISMATCH";
    public const string UnsupportedTemplateVersion = "UNSUPPORTED_TEMPLATE_VERSION";
    public const string TenantMismatch = "TENANT_MISMATCH";
    public const string FileCorrupt = "FILE_CORRUPT";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string FileEmpty = "FILE_EMPTY";
    public const string InvalidFileType = "INVALID_FILE_TYPE";
    public const string QuestionCountMismatch = "QUESTION_COUNT_MISMATCH";
    public const string ImportFileChanged = "IMPORT_FILE_CHANGED";
    public const string ImportAlreadyCompleted = "IMPORT_ALREADY_COMPLETED";
    public const string ImportValidationFailed = "IMPORT_VALIDATION_FAILED";
}

public record ImportIssueDto(
    int RowNumber,
    string Column,
    string Code,
    string Severity,
    string Message
);

public record MatchedStudentDto(
    Guid Id,
    string? StudentNumber,
    string FullName,
    string MatchMethod
);

public record ParsedAggregateSectionDto(
    Guid ExamSectionId,
    Guid SubjectId,
    string SubjectName,
    int Correct,
    int Wrong,
    int Blank,
    decimal CalculatedNet
);

public record ParsedQuestionAnswerDto(
    Guid ExamQuestionId,
    int QuestionNumber,
    string SelectedOption,
    bool IsCorrect,
    bool IsBlank
);

public record ImportRowPreviewDto(
    int RowNumber,
    string StudentIdentifier,
    MatchedStudentDto? MatchedStudent,
    bool IsValid,
    decimal? CalculatedTotalNet,
    List<ParsedAggregateSectionDto> Sections,
    List<ParsedQuestionAnswerDto> Answers
);

public record ExamResultImportPreviewDto(
    string FileHash,
    Guid ExamId,
    ImportType ImportType,
    int TemplateVersion,
    int TotalRows,
    int ValidRowsCount,
    int InvalidRowsCount,
    int WarningCount,
    bool CanImport,
    List<ImportIssueDto> Issues,
    List<ImportRowPreviewDto> Rows
);

public record ExamResultImportCommitResultDto(
    Guid ImportId,
    Guid ExamId,
    string FileHash,
    int ImportedStudentsCount,
    int CreatedAttemptsCount,
    DateTimeOffset ImportedAt
);
