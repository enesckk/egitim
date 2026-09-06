using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;
namespace EgitimPlatform.Modules.Students.Services;
public static class StudentReadAccess
{
    public static bool IsParentOnly(ICurrentUser user) => user.IsInRole(Roles.Parent) && !user.IsSuperAdmin && !user.IsInRole(Roles.InstitutionAdmin) && !user.IsInRole(Roles.Coach) && !user.IsInRole(Roles.Student);
    public static async Task<Student> GetAsync(IApplicationDbContext db, ICurrentUser user, ICoachStudentQuery coaches, Guid studentId, CancellationToken ct)
    {
        var student = await db.Set<Student>().SingleOrDefaultAsync(x => x.Id == studentId, ct) ?? throw new NotFoundException("Student", studentId);
        if (!IsParentOnly(user)) { await GoalAuthorizationHelper.AuthorizeForStudentAsync(student, user, coaches, ct); return student; }
        var institution = await user.GetInstitutionIdAsync();
        if (!user.IsAuthenticated || user.UserId is null || institution is null || institution != student.InstitutionId)
            throw new ForbiddenException("Access denied.");
        var related = await (from link in db.Set<StudentParent>()
            join parent in db.Set<Parent>() on link.ParentId equals parent.Id
            where link.StudentId == studentId && link.InstitutionId == institution && link.IsActive &&
                parent.InstitutionId == institution && parent.UserId == user.UserId
            select link.Id).AnyAsync(ct);
        if (!related) throw new ForbiddenException("Access denied.");
        return student;
    }
}
