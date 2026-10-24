namespace EgitimPlatform.Modules.Reporting.Features.InstitutionalReports;

public record ReportFilterRequest(
    Guid? ClassroomId = null,
    Guid? ExamId = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null
);

public record InstitutionalReportDto(
    string InstitutionName,
    int TotalStudents,
    int TotalExams,
    int TotalAttempts,
    decimal OverallAverageNet,
    decimal OverallAverageAdherence,
    List<BranchReportItemDto> BranchComparisons,
    List<AdherenceDistributionDto> AdherenceDistribution,
    List<SubjectAverageDto> SubjectAverages,
    List<ExamPerformanceRowDto> ExamPerformances
);

public record BranchReportItemDto(
    Guid ClassroomId,
    string BranchName,
    int StudentCount,
    int CoachCount,
    decimal AverageAdherence,
    decimal AverageNet
);

public record AdherenceDistributionDto(
    string Range,
    int StudentCount,
    decimal Percentage,
    string Color
);

public record SubjectAverageDto(
    string SubjectName,
    decimal AverageNet,
    decimal AverageCorrect,
    decimal AverageWrong
);

public record ExamPerformanceRowDto(
    Guid AttemptId,
    string ExamTitle,
    string ClassroomName,
    string StudentName,
    decimal TotalNet,
    decimal Score,
    DateTimeOffset TakenAt
);
