using EgitimPlatform.BuildingBlocks.Authorization;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Students.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
namespace EgitimPlatform.Modules.Students.Features.ManageParents;
public class ParentHandler(IApplicationDbContext db, ICurrentUser user, IUserDirectory users, IAuditService audit)
{
    public async Task<ParentDto> CreateAsync(CreateParentCommand c, CancellationToken ct)
    {
        await new CreateParentValidator().ValidateAndThrowAsync(c, ct);
        await InstitutionManagement.DemandAsync(user, c.InstitutionId);
        if (c.UserId.HasValue && !await users.IsActiveInInstitutionAsync(c.UserId.Value, c.InstitutionId, Roles.Parent, ct))
            throw new ValidationException("Parent account must be active in the same institution with the Parent role.");
        var parent = new Parent { InstitutionId = c.InstitutionId, UserId = c.UserId, FirstName = c.FirstName, LastName = c.LastName, Phone = c.Phone, Email = c.Email, CreatedBy = user.UserId };
        db.Set<Parent>().Add(parent);
        await audit.AddPendingLogAsync(user.UserId!.Value, "Parent.Created", "Parent", parent.Id.ToString(), parent.InstitutionId);
        await db.SaveChangesAsync(ct);
        return new(parent.Id, parent.FirstName, parent.LastName, parent.UserId, parent.Phone, parent.Email);
    }
    public async Task<IReadOnlyList<ParentDto>> ListAsync(ListParentsQuery q, CancellationToken ct = default)
    {
        await new ListParentsQueryValidator().ValidateAndThrowAsync(q, ct);
        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var parents = db.Set<Parent>().AsNoTracking().Where(x => !x.IsDeleted);
        if (!user.IsSuperAdmin)
            parents = parents.Where(x => x.InstitutionId == institutionId!.Value);

        return await parents
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(x => new ParentDto(x.Id, x.FirstName, x.LastName, x.UserId, x.Phone, x.Email))
            .ToListAsync(ct);
    }
    public async Task<ParentDto> GetAsync(Guid parentId, CancellationToken ct)
    {
        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var parents = db.Set<Parent>().AsNoTracking().Where(x => x.Id == parentId);
        if (!user.IsSuperAdmin)
            parents = parents.Where(x => x.InstitutionId == institutionId!.Value);

        return await parents
            .Select(x => new ParentDto(x.Id, x.FirstName, x.LastName, x.UserId, x.Phone, x.Email))
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Parent", parentId);
    }
    public async Task<IReadOnlyList<StudentParentDto>> ListForStudentAsync(Guid studentId, CancellationToken ct)
    {
        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var students = db.Set<Student>().AsNoTracking().Where(x => x.Id == studentId);
        if (!user.IsSuperAdmin)
            students = students.Where(x => x.InstitutionId == institutionId!.Value);

        var student = await students.SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Student", studentId);

        return await (from link in db.Set<StudentParent>().AsNoTracking()
                      join parent in db.Set<Parent>().AsNoTracking() on link.ParentId equals parent.Id
                      where link.StudentId == studentId && link.InstitutionId == student.InstitutionId && parent.InstitutionId == student.InstitutionId
                      orderby link.CreatedAt, link.Id
                      select new StudentParentDto(
                          link.Id,
                          link.StudentId,
                          link.ParentId,
                          link.RelationshipType,
                          link.IsActive,
                          parent.FirstName + " " + parent.LastName))
            .ToListAsync(ct);
    }
    public async Task<StudentParentDto> LinkAsync(LinkParentCommand c, CancellationToken ct)
    {
        await new LinkParentValidator().ValidateAndThrowAsync(c, ct);
        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var students = db.Set<Student>().Where(x => x.Id == c.StudentId);
        if (!user.IsSuperAdmin)
            students = students.Where(x => x.InstitutionId == institutionId!.Value);

        var student = await students.SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Student", c.StudentId);
        await InstitutionManagement.DemandAsync(user, student.InstitutionId);
        var parent = await db.Set<Parent>().SingleOrDefaultAsync(x => x.Id == c.ParentId && x.InstitutionId == student.InstitutionId, ct)
            ?? throw new NotFoundException("Parent", c.ParentId);
        if (await db.Set<StudentParent>().AnyAsync(x => x.StudentId == student.Id && x.ParentId == parent.Id && x.InstitutionId == student.InstitutionId && x.IsActive, ct))
            throw new ConflictException("An active relationship already exists.");
        var link = new StudentParent { StudentId = student.Id, ParentId = parent.Id, InstitutionId = student.InstitutionId, RelationshipType = c.RelationshipType, CreatedBy = user.UserId };
        db.Set<StudentParent>().Add(link);
        await audit.AddPendingLogAsync(user.UserId!.Value, "StudentParent.Linked", "StudentParent", link.Id.ToString(), link.InstitutionId);
        await db.SaveChangesAsync(ct);
        return ToDto(link);
    }
    public async Task<StudentParentDto> SetActiveAsync(SetParentRelationshipCommand c, CancellationToken ct)
    {
        await new SetParentRelationshipValidator().ValidateAndThrowAsync(c, ct);
        var institutionId = await user.GetInstitutionIdAsync();
        if (!user.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Access denied.");

        var links = db.Set<StudentParent>().Where(x => x.Id == c.RelationshipId);
        if (!user.IsSuperAdmin)
            links = links.Where(x => x.InstitutionId == institutionId!.Value);

        var link = await links.SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("StudentParent", c.RelationshipId);
        await InstitutionManagement.DemandAsync(user, link.InstitutionId);
        if (c.IsActive && (!await db.Set<Student>().AnyAsync(x => x.Id == link.StudentId && x.InstitutionId == link.InstitutionId, ct) ||
            !await db.Set<Parent>().AnyAsync(x => x.Id == link.ParentId && x.InstitutionId == link.InstitutionId, ct)))
            throw new ConflictException("Relationship participants must be non-deleted records in the same institution.");
        if (c.IsActive && await db.Set<StudentParent>().AnyAsync(x => x.Id != link.Id && x.StudentId == link.StudentId &&
            x.ParentId == link.ParentId && x.InstitutionId == link.InstitutionId && x.IsActive, ct))
            throw new ConflictException("An active relationship already exists.");
        link.IsActive = c.IsActive; link.UpdatedBy = user.UserId;
        await audit.AddPendingLogAsync(user.UserId!.Value, c.IsActive ? "StudentParent.Activated" : "StudentParent.Deactivated", "StudentParent", link.Id.ToString(), link.InstitutionId);
        await db.SaveChangesAsync(ct);
        return ToDto(link);
    }
    private static StudentParentDto ToDto(StudentParent x) => new(x.Id, x.StudentId, x.ParentId, x.RelationshipType, x.IsActive);
}
