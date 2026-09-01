using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace EgitimPlatform.Modules.Identity.Infrastructure;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Load all module assemblies explicitly (AppDomain scanning doesn't work at design time)
        var moduleNames = new[]
        {
            "EgitimPlatform.Modules.Institutions",
            "EgitimPlatform.Modules.Students",
            "EgitimPlatform.Modules.Coaching",
        };

        foreach (var moduleName in moduleNames)
        {
            try
            {
                var assembly = Assembly.Load(moduleName);
                builder.ApplyConfigurationsFromAssembly(assembly);
            }
            catch (Exception)
            {
                // Module assembly not available — skip
            }
        }

        // Explicit query filters for Identity entities
        builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsDeleted);
        builder.Entity<ApplicationRole>().HasQueryFilter(r => !r.IsDeleted);
        builder.Entity<Permission>().HasQueryFilter(p => !p.IsDeleted);

        // Auto-apply soft-delete filter to ALL entities implementing ISoftDeletable
        // This covers Student, Coach, Parent, Institution from other modules
        // without requiring direct type references (avoids circular dependencies).
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

            // Skip if filter already applied (Identity entities above)
            if (entityType.GetDeclaredQueryFilters().Any()) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var notDeleted = Expression.Not(property);
            var lambda = Expression.Lambda(notDeleted, parameter);

            entityType.SetQueryFilter(lambda);
        }

        // Cross-module FK constraints via reflection
        // (modules don't reference each other directly, so we configure here)
        ApplyCrossModuleForeignKeys(builder);
    }

    private static void ApplyCrossModuleForeignKeys(ModelBuilder builder)
    {
        var institutionType = FindEntityType(builder, "Institution");
        var studentType = FindEntityType(builder, "Student");
        var coachType = FindEntityType(builder, "Coach");
        var parentType = FindEntityType(builder, "Parent");
        var assignmentType = FindEntityType(builder, "StudentCoachAssignment");

        // Student → Institution FK
        if (studentType is not null && institutionType is not null)
        {
            builder.Entity(studentType.ClrType)
                .HasOne(institutionType.ClrType)
                .WithMany()
                .HasForeignKey("InstitutionId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        // Coach → Institution FK
        if (coachType is not null && institutionType is not null)
        {
            builder.Entity(coachType.ClrType)
                .HasOne(institutionType.ClrType)
                .WithMany()
                .HasForeignKey("InstitutionId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        // Parent → Institution FK
        if (parentType is not null && institutionType is not null)
        {
            builder.Entity(parentType.ClrType)
                .HasOne(institutionType.ClrType)
                .WithMany()
                .HasForeignKey("InstitutionId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        // StudentCoachAssignment → Student FK
        if (assignmentType is not null && studentType is not null)
        {
            builder.Entity(assignmentType.ClrType)
                .HasOne(studentType.ClrType)
                .WithMany()
                .HasForeignKey("StudentId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        // StudentCoachAssignment → Coach FK
        if (assignmentType is not null && coachType is not null)
        {
            builder.Entity(assignmentType.ClrType)
                .HasOne(coachType.ClrType)
                .WithMany()
                .HasForeignKey("CoachId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        // StudentCoachAssignment → Institution FK
        if (assignmentType is not null && institutionType is not null)
        {
            builder.Entity(assignmentType.ClrType)
                .HasOne(institutionType.ClrType)
                .WithMany()
                .HasForeignKey("InstitutionId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        // P1-06: Filtered unique index for one active primary coach per student
        if (assignmentType is not null)
        {
            builder.Entity(assignmentType.ClrType)
                .HasIndex("StudentId", "IsPrimary")
                .IsUnique()
                .HasFilter("[IsPrimary] = 1 AND [IsActive] = 1");
        }
    }

    private static IMutableEntityType? FindEntityType(ModelBuilder builder, string typeName)
    {
        return builder.Model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.Name == typeName);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
