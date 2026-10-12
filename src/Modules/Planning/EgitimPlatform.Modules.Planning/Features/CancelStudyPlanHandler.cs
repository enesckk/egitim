using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features;

public class CancelStudyPlanHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory)
{
    public async Task HandleAsync(Guid studentId, Guid planId, CancellationToken ct = default)
    {
        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student not found.");

        await ValidateAccessAsync(student);

        var plan = await db.Set<StudyPlan>()
            .FirstOrDefaultAsync(p => p.Id == planId && p.StudentId == studentId && p.InstitutionId == student.InstitutionId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Study plan not found.");

        if (plan.Status == StudyPlanStatus.Cancelled)
        {
            return;
        }

        plan.Status = StudyPlanStatus.Cancelled;
        await db.SaveChangesAsync(ct);
    }

    private async Task ValidateAccessAsync(StudentReferenceDto student)
    {
        if (currentUser.IsInRole(Roles.Parent))
        {
            throw new ForbiddenException("Parent cannot cancel student study plans.");
        }

        if (currentUser.IsInRole(Roles.Coach))
        {
            throw new ForbiddenException("Coach cannot modify student study plans.");
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Teacher role cannot modify student study plans.");
        }

        if (currentUser.IsSuperAdmin) return;

        var currentInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentInstId.HasValue || currentInstId.Value != student.InstitutionId)
        {
            throw new ForbiddenException("Access denied across institution boundary.");
        }

        if (currentUser.IsInRole(Roles.Student) && currentUser.UserId != student.UserId)
        {
            throw new ForbiddenException("Student can only cancel their own study plan.");
        }
    }
}
