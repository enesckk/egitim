namespace EgitimPlatform.Modules.Exams.Features.CreateManualResult;

public record ManualSectionResultInputDto(
    Guid ExamSectionId,
    int Correct,
    int Wrong,
    int Blank
);

public record CreateSingleManualResultCommand(
    Guid StudentId,
    List<ManualSectionResultInputDto> Sections
);

public record SingleManualResultResponseDto(
    Guid AttemptId,
    Guid StudentId,
    Guid ExamId,
    int AttemptNumber,
    int TotalCorrect,
    int TotalWrong,
    int TotalBlank,
    decimal TotalNet,
    bool IsRawAvailable,
    DateTimeOffset FinalizedAt
);

public record BulkStudentManualResultItemDto(
    Guid StudentId,
    List<ManualSectionResultInputDto> Sections
);

public record CreateBulkManualResultCommand(
    List<BulkStudentManualResultItemDto> StudentResults
);

public record BulkManualResultResponseDto(
    Guid ExamId,
    int StudentsProcessed,
    int AttemptsCreated,
    int SubjectResultsCreated,
    List<Guid> AttemptIds
);
