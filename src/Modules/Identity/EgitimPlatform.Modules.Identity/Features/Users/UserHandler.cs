using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Identity.Features.Users;

public class UserHandler(
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser,
    IAuditService audit)
{
    private static readonly HashSet<string> InstitutionAdminAllowedRoles = new(StringComparer.Ordinal)
    {
        Roles.Student,
        Roles.Teacher,
        Roles.Coach,
        Roles.Parent
    };

    private static readonly HashSet<string> SuperAdminAllowedRoles = new(StringComparer.Ordinal)
    {
        Roles.InstitutionAdmin,
        Roles.Student,
        Roles.Teacher,
        Roles.Coach,
        Roles.Parent
    };

    public async Task<UserDto> CreateAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        await new CreateUserValidator().ValidateAndThrowAsync(command, ct);

        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenException("Access denied.");

        // 1. Validate Role & Prevent Privilege Escalation
        if (string.Equals(command.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("SuperAdmin accounts cannot be provisioned via the user management API.");

        if (!Roles.All.Contains(command.Role, StringComparer.Ordinal))
            throw new ValidationException($"Unsupported role '{command.Role}'.");

        if (currentUser.IsSuperAdmin)
        {
            if (!SuperAdminAllowedRoles.Contains(command.Role))
                throw new ForbiddenException($"Cannot assign role '{command.Role}'.");
        }
        else if (currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            if (!InstitutionAdminAllowedRoles.Contains(command.Role))
                throw new ForbiddenException("Institution administrators can only create Student, Teacher, Coach, or Parent accounts.");
        }
        else
        {
            throw new ForbiddenException("Only administrators can create user accounts.");
        }

        // 2. Resolve & Enforce Tenant Scope
        Guid targetInstitutionId;
        if (currentUser.IsSuperAdmin)
        {
            var callerInstId = await currentUser.GetInstitutionIdAsync();
            targetInstitutionId = command.InstitutionId
                ?? callerInstId
                ?? throw new ValidationException("InstitutionId is required when creating a user as SuperAdmin.");
        }
        else
        {
            var callerInstId = await currentUser.GetInstitutionIdAsync()
                ?? throw new ForbiddenException("Institution context required.");

            if (command.InstitutionId.HasValue && command.InstitutionId.Value != callerInstId)
                throw new ForbiddenException("Cross-institution user creation is not allowed.");

            targetInstitutionId = callerInstId;
        }

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var emailExists = await db.Set<ApplicationUser>()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, ct);
        if (emailExists)
            throw new ConflictException($"A user with email '{command.Email.Trim()}' already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = command.Email.Trim(),
            Email = command.Email.Trim(),
            EmailConfirmed = true,
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            InstitutionId = targetInstitutionId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
            if (createResult.Errors.Any(e => e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
                throw new ConflictException(errors);
            throw new ValidationException(errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, command.Role);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
            throw new ValidationException(errors);
        }

        await audit.AddPendingLogAsync(
            userId: currentUser.UserId.Value,
            action: "User.Created",
            entityType: "ApplicationUser",
            entityId: user.Id.ToString(),
            institutionId: targetInstitutionId,
            metadataJson: $"{{\"role\":\"{command.Role}\"}}");

        await db.SaveChangesAsync(ct);

        return new UserDto(
            user.Id,
            user.Email ?? command.Email.Trim(),
            user.FirstName,
            user.LastName,
            command.Role,
            user.InstitutionId,
            user.IsActive,
            user.CreatedAt);
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(ListUsersQuery query, CancellationToken ct = default)
    {
        await new ListUsersQueryValidator().ValidateAndThrowAsync(query, ct);

        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new ForbiddenException("Access denied.");

        if (!currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
            throw new ForbiddenException("Access denied.");

        var institutionId = await currentUser.GetInstitutionIdAsync();
        if (!currentUser.IsSuperAdmin && !institutionId.HasValue)
            throw new ForbiddenException("Institution context required.");

        var baseQuery = from u in db.Set<ApplicationUser>().AsNoTracking()
                        join ur in db.Set<IdentityUserRole<Guid>>().AsNoTracking() on u.Id equals ur.UserId
                        join r in db.Set<ApplicationRole>().AsNoTracking() on ur.RoleId equals r.Id
                        where !u.IsDeleted && !r.IsDeleted
                        select new
                        {
                            u.Id,
                            Email = u.Email ?? string.Empty,
                            u.FirstName,
                            u.LastName,
                            Role = r.Name ?? string.Empty,
                            u.InstitutionId,
                            u.IsActive,
                            u.CreatedAt
                        };

        if (!currentUser.IsSuperAdmin)
        {
            baseQuery = baseQuery.Where(x => x.InstitutionId == institutionId!.Value && x.Role != Roles.SuperAdmin);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var roleFilter = query.Role.Trim();
            baseQuery = baseQuery.Where(x => x.Role == roleFilter);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            baseQuery = baseQuery.Where(x =>
                x.FirstName.Contains(term) ||
                x.LastName.Contains(term) ||
                x.Email.Contains(term));
        }

        return await baseQuery
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new UserDto(
                x.Id,
                x.Email,
                x.FirstName,
                x.LastName,
                x.Role,
                x.InstitutionId,
                x.IsActive,
                x.CreatedAt))
            .ToListAsync(ct);
    }
}
