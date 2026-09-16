namespace EgitimPlatform.Modules.Students.Features.ListStudentGoals;

public sealed record ListStudentGoalsQuery(Guid StudentId, bool? IsActive = null);
