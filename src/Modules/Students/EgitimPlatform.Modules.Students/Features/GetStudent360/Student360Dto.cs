using EgitimPlatform.BuildingBlocks.Interfaces;
namespace EgitimPlatform.Modules.Students.Features.GetStudent360;
// Explicit whitelist: descriptions/notes are not part of the parent-safe composed contract.
public record CurrentGoalDto(Guid Id, string Title, Guid? TargetExamTypeId, int? TargetScore, int? TargetRank, string? TargetSchoolName, DateTimeOffset EffectiveDate);
public record ParentRelationshipDto(Guid ParentId, string Name, string RelationshipType);
public record Student360Dto(StudentDto Student, IReadOnlyList<CurrentGoalDto> CurrentGoals,
    IReadOnlyList<StudentCoachSummaryDto> Coaches, IReadOnlyList<ParentRelationshipDto> Parents,
    IReadOnlyList<AcademicReferenceDto> ExamTypes);
