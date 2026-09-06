using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using EgitimPlatform.Modules.Students.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
namespace EgitimPlatform.Modules.Students.Features.GetStudent360;
public record GetStudent360Query(Guid StudentId);
public class GetStudent360Validator : AbstractValidator<GetStudent360Query>
{
    public GetStudent360Validator() => RuleFor(x => x.StudentId).NotEmpty();
}
public class GetStudent360Handler(IApplicationDbContext db, ICurrentUser user, ICoachStudentQuery assignments,
    IStudentCoachSummaryQuery coaches, IAcademicCatalog academic)
{
    public async Task<Student360Dto> HandleAsync(GetStudent360Query query, CancellationToken ct = default)
    {
        await new GetStudent360Validator().ValidateAndThrowAsync(query, ct);
        var student = await StudentReadAccess.GetAsync(db, user, assignments, query.StudentId, ct);
        var goals = await db.Set<StudentGoal>().AsNoTracking()
            .Where(x => x.StudentId == student.Id && x.InstitutionId == student.InstitutionId && x.IsActive)
            .OrderByDescending(x => x.EffectiveDate).ThenBy(x => x.Id)
            .Select(x => new CurrentGoalDto(x.Id, x.Title, x.TargetExamTypeId, x.TargetScore, x.TargetRank, x.TargetSchoolName, x.EffectiveDate)).ToListAsync(ct);
        var parentsQuery = from link in db.Set<StudentParent>().AsNoTracking()
                           join parent in db.Set<Parent>() on link.ParentId equals parent.Id
                           where link.StudentId == student.Id && link.InstitutionId == student.InstitutionId &&
                                 parent.InstitutionId == student.InstitutionId && link.IsActive
                           select new { parent.Id, parent.FirstName, parent.LastName, parent.UserId, link.RelationshipType };
        // A parent may see their own relationship, not details about other guardians.
        if (StudentReadAccess.IsParentOnly(user)) parentsQuery = parentsQuery.Where(x => x.UserId == user.UserId);
        var parents = await parentsQuery.OrderBy(x => x.Id).Select(x => new ParentRelationshipDto(x.Id, x.FirstName + " " + x.LastName, x.RelationshipType)).ToListAsync(ct);
        var exams = new List<AcademicReferenceDto>();
        foreach (var id in goals.Where(x => x.TargetExamTypeId.HasValue).Select(x => x.TargetExamTypeId!.Value).Distinct())
        {
            var exam = await academic.GetExamTypeAsync(id, ct);
            if (exam is not null) exams.Add(exam);
        }
        return new(student.ToDto(), goals, await coaches.GetAsync(student.Id, student.InstitutionId, ct), parents, exams);
    }
}
