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
        return new(parent.Id, parent.FirstName, parent.LastName);
    }
    public async Task<StudentParentDto> LinkAsync(LinkParentCommand c, CancellationToken ct)
    {
        await new LinkParentValidator().ValidateAndThrowAsync(c, ct);
        var student = await db.Set<Student>().SingleOrDefaultAsync(x => x.Id == c.StudentId, ct) ?? throw new NotFoundException("Student", c.StudentId);
        await InstitutionManagement.DemandAsync(user, student.InstitutionId);
        var parent = await db.Set<Parent>().SingleOrDefaultAsync(x => x.Id == c.ParentId && x.InstitutionId == student.InstitutionId, ct)
            ?? throw new ForbiddenException("Parent is unavailable in this institution.");
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
        var link = await db.Set<StudentParent>().SingleOrDefaultAsync(x => x.Id == c.RelationshipId, ct) ?? throw new NotFoundException("StudentParent", c.RelationshipId);
        await InstitutionManagement.DemandAsync(user, link.InstitutionId);
        if (c.IsActive && (!await db.Set<Student>().AnyAsync(x => x.Id == link.StudentId && x.InstitutionId == link.InstitutionId, ct) ||
            !await db.Set<Parent>().AnyAsync(x => x.Id == link.ParentId && x.InstitutionId == link.InstitutionId, ct)))
            throw new ConflictException("Relationship participants must be active records.");
        link.IsActive = c.IsActive; link.UpdatedBy = user.UserId;
        await audit.AddPendingLogAsync(user.UserId!.Value, c.IsActive ? "StudentParent.Activated" : "StudentParent.Deactivated", "StudentParent", link.Id.ToString(), link.InstitutionId);
        await db.SaveChangesAsync(ct);
        return ToDto(link);
    }
    private static StudentParentDto ToDto(StudentParent x) => new(x.Id, x.StudentId, x.ParentId, x.RelationshipType, x.IsActive);
}
