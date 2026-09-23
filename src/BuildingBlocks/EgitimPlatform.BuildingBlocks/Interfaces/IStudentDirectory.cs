namespace EgitimPlatform.BuildingBlocks.Interfaces;

public record StudentReferenceDto(Guid Id, Guid InstitutionId, Guid? UserId);

public interface IStudentDirectory
{
    Task<StudentReferenceDto?> GetStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<bool> IsParentRelatedToStudentAsync(Guid parentUserId, Guid studentId, Guid institutionId, CancellationToken ct = default);
}
