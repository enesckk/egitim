using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace EgitimPlatform.Infrastructure;

/// <summary>
/// P2-12: Shared platform persistence — moved out of Identity module into neutral Infrastructure layer.
/// Domain modules access the database through IApplicationDbContext (BuildingBlocks interface),
/// NOT through this concrete type. This decouples module boundaries from persistence ownership.
///
/// Module entity configurations are discovered at runtime via Assembly.Load —
/// modules don't need compile-time references to this project.
/// </summary>
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

        // Identity module configurations (this assembly's parent reference)
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Load Identity module configurations
        TryApplyConfigurationsFromAssembly(builder, "EgitimPlatform.Modules.Identity");

        // Load domain module configurations via runtime assembly discovery.
        // This avoids compile-time project references (no circular dependencies).
        var moduleNames = new[]
        {
            "EgitimPlatform.Modules.Institutions",
            "EgitimPlatform.Modules.Students",
            "EgitimPlatform.Modules.Coaching",
        };

        foreach (var moduleName in moduleNames)
        {
            TryApplyConfigurationsFromAssembly(builder, moduleName);
        }

        // Explicit query filters for Identity entities
        builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsDeleted);
        builder.Entity<ApplicationRole>().HasQueryFilter(r => !r.IsDeleted);
        builder.Entity<Permission>().HasQueryFilter(p => !p.IsDeleted);

        // Auto-apply soft-delete filter to ALL entities implementing ISoftDeletable
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;
            if (entityType.GetDeclaredQueryFilters().Any()) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var notDeleted = Expression.Not(property);
            var lambda = Expression.Lambda(notDeleted, parameter);

            entityType.SetQueryFilter(lambda);
        }

        // Cross-module FK constraints via reflection
        ApplyCrossModuleForeignKeys(builder);
    }

    private static void TryApplyConfigurationsFromAssembly(ModelBuilder builder, string assemblyName)
    {
        try
        {
            var assembly = Assembly.Load(assemblyName);
            builder.ApplyConfigurationsFromAssembly(assembly);
        }
        catch (Exception)
        {
            // Module assembly not available — skip
        }
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

        // P2-6: Cross-tenant composite FKs for StudentCoachAssignment.
        // StudentCoachAssignment → Student (composite: StudentId + InstitutionId)
        if (assignmentType is not null && studentType is not null)
        {
            builder.Entity(assignmentType.ClrType)
                .HasOne(studentType.ClrType)
                .WithMany()
                .HasForeignKey("StudentId", "InstitutionId")
                .HasPrincipalKey("Id", "InstitutionId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        // StudentCoachAssignment → Coach (composite: CoachId + InstitutionId)
        if (assignmentType is not null && coachType is not null)
        {
            builder.Entity(assignmentType.ClrType)
                .HasOne(coachType.ClrType)
                .WithMany()
                .HasForeignKey("CoachId", "InstitutionId")
                .HasPrincipalKey("Id", "InstitutionId")
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
