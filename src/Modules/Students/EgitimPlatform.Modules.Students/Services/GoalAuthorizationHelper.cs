using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Students.Services;

/// <summary>
/// Shared authorization logic for StudentGoal operations.
/// Mirrors the authorization pattern for Student access.
/// </summary>
internal static class GoalAuthorizationHelper
{
    public static async Task AuthorizeForStudentAsync(
        Student student,
        ICurrentUser currentUser,
        ICoachStudentQuery coachStudentQuery,
        CancellationToken ct)
    {
        var institutionId = await currentUser.GetInstitutionIdAsync();

        if (currentUser.IsSuperAdmin) return;

        if (currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            if (!institutionId.HasValue || student.InstitutionId != institutionId.Value)
                throw new ForbiddenException("Access denied.");
            return;
        }

        if (currentUser.IsInRole(Roles.Coach))
        {
            if (currentUser.UserId is null || !institutionId.HasValue)
                throw new ForbiddenException("Access denied.");

            if (student.InstitutionId != institutionId.Value)
                throw new ForbiddenException("Access denied.");

            var hasAssignment = await coachStudentQuery.HasActiveAssignmentAsync(
                currentUser.UserId.Value, institutionId.Value, student.Id, ct);
            if (!hasAssignment)
                throw new ForbiddenException("Access denied.");
            return;
        }

        if (currentUser.IsInRole(Roles.Student))
        {
            if (currentUser.UserId is null || student.UserId != currentUser.UserId.Value)
                throw new ForbiddenException("Access denied.");
            return;
        }

        throw new ForbiddenException("Access denied.");
    }

    public static async Task<Student> FetchStudentOrThrowAsync(
        IApplicationDbContext dbContext,
        Guid studentId,
        CancellationToken ct)
    {
        var student = await dbContext.Set<Student>()
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);
        return student ?? throw new EgitimPlatform.BuildingBlocks.Exceptions.NotFoundException("Student", studentId);
    }
}
