using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Identity.Auth;

public sealed class UserProvisioner(
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IAuditService audit,
    ICurrentUser currentUser) : IUserProvisioner
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
    {
        Roles.Student,
        Roles.Teacher,
        Roles.Coach,
        Roles.Parent
    };

    public async Task<Guid> ProvisionUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        Guid institutionId,
        CancellationToken ct = default)
    {
        if (string.Equals(role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, Roles.InstitutionAdmin, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Privileged administrative roles cannot be provisioned via profile creation.");
        }

        if (!AllowedRoles.Contains(role))
        {
            throw new ValidationException($"Unsupported role '{role}' for user provisioning.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ValidationException("A valid email address is required.");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new ValidationException("Password must be at least 8 characters.");

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var emailExists = await db.Set<ApplicationUser>()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, ct);
        if (emailExists)
            throw new ConflictException($"A user with email '{email.Trim()}' already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email.Trim(),
            Email = email.Trim(),
            EmailConfirmed = true,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            InstitutionId = institutionId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
            if (createResult.Errors.Any(e => e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
                throw new ConflictException(errors);
            throw new ValidationException(errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
            throw new ValidationException(errors);
        }

        if (currentUser.UserId.HasValue)
        {
            await audit.AddPendingLogAsync(
                userId: currentUser.UserId.Value,
                action: "User.Provisioned",
                entityType: "ApplicationUser",
                entityId: user.Id.ToString(),
                institutionId: institutionId,
                metadataJson: $"{{\"role\":\"{role}\"}}");
        }

        return user.Id;
    }

    public async Task DeleteProvisionedUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            await userManager.DeleteAsync(user);
        }
    }
}
