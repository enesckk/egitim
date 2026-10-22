using EgitimPlatform.BuildingBlocks.Authorization;
using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Students.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Coaching.Features.ManageCoaches;

public sealed record CreateCoachCommand(
    Guid InstitutionId,
    Guid? UserId,
    string FirstName,
    string LastName,
    string? Email = null,
    string? Password = null);

public sealed record ListCoachesQuery(
    int Page = 1,
    int PageSize = 50);

public sealed record CoachAssignedStudentDto(
    Guid StudentId,
    string FullName,
    int? GradeLevel,
    bool IsPrimary);

public sealed record CoachSummaryDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string FullName,
    Guid InstitutionId,
    int AssignedStudentCount,
    IReadOnlyList<CoachAssignedStudentDto> AssignedStudents);

public class CreateCoachValidator : AbstractValidator<CreateCoachCommand>
{
    public CreateCoachValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty().When(x => string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Password).MinimumLength(8).When(x => !string.IsNullOrEmpty(x.Password));
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class ListCoachesQueryValidator : AbstractValidator<ListCoachesQuery>
{
    public ListCoachesQueryValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 100000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}

public class CoachManagementHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IUserDirectory users,
    IAuditService audit,
    IUserProvisioner? userProvisioner = null)
{
    public async Task<CoachSummaryDto> CreateAsync(CreateCoachCommand command, CancellationToken ct = default)
    {
        await new CreateCoachValidator().ValidateAndThrowAsync(command, ct);
        await InstitutionManagement.DemandAsync(currentUser, command.InstitutionId);

        Guid targetUserId;
        Guid? provisionedUserId = null;

        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            if (command.UserId.HasValue)
            {
                if (!await users.IsActiveInInstitutionAsync(command.UserId.Value, command.InstitutionId, Roles.Coach, ct))
                    throw new ValidationException("Coach account must be active in the same institution with the Coach role.");

                var exists = await db.Set<Coach>()
                    .AnyAsync(c => c.UserId == command.UserId.Value && c.InstitutionId == command.InstitutionId && !c.IsDeleted, ct);
                if (exists)
                    throw new ConflictException("A coach profile for this user already exists in this institution.");

                targetUserId = command.UserId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(command.Email) && !string.IsNullOrWhiteSpace(command.Password))
            {
                if (userProvisioner is null)
                    throw new InvalidOperationException("User provisioning service is unavailable.");

                provisionedUserId = await userProvisioner.ProvisionUserAsync(
                    command.Email,
                    command.Password,
                    command.FirstName,
                    command.LastName,
                    Roles.Coach,
                    command.InstitutionId,
                    ct);

                targetUserId = provisionedUserId.Value;
            }
            else
            {
                throw new ValidationException("Either UserId or Email and Password must be provided.");
            }

            var coach = new Coach
            {
                InstitutionId = command.InstitutionId,
                UserId = targetUserId,
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName.Trim(),
                CreatedBy = currentUser.UserId
            };

            db.Set<Coach>().Add(coach);
            await audit.AddPendingLogAsync(
                currentUser.UserId!.Value,
                "Coach.Created",
                "Coach",
                coach.Id.ToString(),
                coach.InstitutionId);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new CoachSummaryDto(
                coach.Id,
                coach.UserId,
                coach.FirstName,
                coach.LastName,
                $"{coach.FirstName} {coach.LastName}",
                coach.InstitutionId,
                0,
                []);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            if (provisionedUserId.HasValue && userProvisioner is not null)
            {
                try
                {
                    await userProvisioner.DeleteProvisionedUserAsync(provisionedUserId.Value, ct);
                }
                catch
                {
                    // Secondary cleanup attempt; DB transaction rollback already guaranteed database consistency.
                }
            }
            throw;
        }
    }

    public async Task<IReadOnlyList<CoachSummaryDto>> ListAsync(ListCoachesQuery query, CancellationToken ct = default)
    {
        await new ListCoachesQueryValidator().ValidateAndThrowAsync(query, ct);

        if (!currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
            throw new ForbiddenException("Access denied.");

        var institutionId = await currentUser.GetInstitutionIdAsync();
        if (!currentUser.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Institution context required.");

        var coachesQuery = db.Set<Coach>().AsNoTracking().Where(c => !c.IsDeleted);
        if (!currentUser.IsSuperAdmin)
            coachesQuery = coachesQuery.Where(c => c.InstitutionId == institutionId!.Value);

        var coaches = await coachesQuery
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ThenBy(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        if (coaches.Count == 0)
            return [];

        var coachIds = coaches.Select(c => c.Id).ToList();

        var assignments = await (from a in db.Set<StudentCoachAssignment>().AsNoTracking()
                                 join s in db.Set<Student>().AsNoTracking() on a.StudentId equals s.Id
                                 where coachIds.Contains(a.CoachId) && a.IsActive && !s.IsDeleted && a.InstitutionId == s.InstitutionId
                                 orderby s.LastName, s.FirstName
                                 select new
                                 {
                                     a.CoachId,
                                     StudentId = s.Id,
                                     FullName = s.FirstName + " " + s.LastName,
                                     s.GradeLevel,
                                     a.IsPrimary
                                 }).ToListAsync(ct);

        var assignmentsByCoach = assignments
            .GroupBy(x => x.CoachId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CoachAssignedStudentDto>)g.Select(x => new CoachAssignedStudentDto(
                    x.StudentId,
                    x.FullName,
                    x.GradeLevel,
                    x.IsPrimary)).ToList());

        return coaches.Select(c =>
        {
            var assigned = assignmentsByCoach.GetValueOrDefault(c.Id, []);
            return new CoachSummaryDto(
                c.Id,
                c.UserId,
                c.FirstName,
                c.LastName,
                $"{c.FirstName} {c.LastName}",
                c.InstitutionId,
                assigned.Count,
                assigned);
        }).ToList();
    }
}
