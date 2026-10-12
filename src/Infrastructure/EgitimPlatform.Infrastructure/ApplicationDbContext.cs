using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections;
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
            "EgitimPlatform.Modules.Teachers",
            "EgitimPlatform.Modules.Academic",
            "EgitimPlatform.Modules.Exams",
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
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Required module {assemblyName} could not be configured.", ex);
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

        // P1-01 CLOSURE: Coach.UserId → ApplicationUser.Id FK.
        // Real DB-level reference ensures every coach profile maps to a real user.
        // Combined with the filtered unique index on (UserId, InstitutionId),
        // this prevents orphaned or ambiguous coach profiles.
        if (coachType is not null)
        {
            builder.Entity(coachType.ClrType)
                .HasOne(typeof(ApplicationUser))
                .WithMany()
                .HasForeignKey("UserId")
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

        // Sprint 2: Parent.UserId → ApplicationUser.Id FK (nullable — not all parents have user accounts).
        if (parentType is not null)
        {
            builder.Entity(parentType.ClrType)
                .HasOne(typeof(ApplicationUser))
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        // Sprint 2: Teacher entity + Teacher.UserId → ApplicationUser.Id FK.
        var teacherType = FindEntityType(builder, "Teacher");
        if (teacherType is not null)
        {
            builder.Entity(teacherType.ClrType)
                .HasOne(typeof(ApplicationUser))
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict);

            // Teacher → Institution FK
            if (institutionType is not null)
            {
                builder.Entity(teacherType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        var goalType = FindEntityType(builder, "StudentGoal");
        var examType = FindEntityType(builder, "ExamType");
        var teacherSubjectType = FindEntityType(builder, "TeacherSubject");
        var subjectType = FindEntityType(builder, "Subject");
        var academicTopicType = FindEntityType(builder, "Topic");
        var examEntityType = FindEntityType(builder, "Exam");
        var examSectionType = FindEntityType(builder, "ExamSection");
        var examAttemptType = FindEntityType(builder, "StudentExamAttempt");
        var examTopicResultType = FindEntityType(builder, "StudentExamTopicResult");
        if (goalType is not null && examType is not null)
            builder.Entity(goalType.ClrType).HasOne(examType.ClrType).WithMany()
                .HasForeignKey("TargetExamTypeId").OnDelete(DeleteBehavior.Restrict);
        if (teacherSubjectType is not null && subjectType is not null)
            builder.Entity(teacherSubjectType.ClrType).HasOne(subjectType.ClrType).WithMany()
                .HasForeignKey("SubjectId").OnDelete(DeleteBehavior.Restrict);

        // Sprint 3 Exams cross-module constraints. Same-module composite FKs are
        // configured inside Exams; reflection keeps Infrastructure as the shared
        // persistence owner without introducing direct module references.
        if (examEntityType is not null && institutionType is not null)
            builder.Entity(examEntityType.ClrType).HasOne(institutionType.ClrType).WithMany()
                .HasForeignKey("InstitutionId").OnDelete(DeleteBehavior.Restrict);
        if (examEntityType is not null && examType is not null)
            builder.Entity(examEntityType.ClrType).HasOne(examType.ClrType).WithMany()
                .HasForeignKey("ExamTypeId").OnDelete(DeleteBehavior.Restrict);
        if (examSectionType is not null && subjectType is not null)
            builder.Entity(examSectionType.ClrType).HasOne(subjectType.ClrType).WithMany()
                .HasForeignKey("SubjectId").OnDelete(DeleteBehavior.Restrict);
        if (examAttemptType is not null && studentType is not null)
            builder.Entity(examAttemptType.ClrType).HasOne(studentType.ClrType).WithMany()
                .HasForeignKey("StudentId", "InstitutionId")
                .HasPrincipalKey("Id", "InstitutionId")
                .OnDelete(DeleteBehavior.Restrict);
        if (examTopicResultType is not null && academicTopicType is not null)
            builder.Entity(examTopicResultType.ClrType).HasOne(academicTopicType.ClrType).WithMany()
                .HasForeignKey("TopicId").OnDelete(DeleteBehavior.Restrict);

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

    private EvidenceProtectionPlan PrepareEvidenceProtection()
    {
        ChangeTracker.DetectChanges();
        var changedResultAttemptIds = new HashSet<Guid>();
        var addedAttemptIds = new HashSet<Guid>();
        Type? attemptType = null;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IImmutableHistory && entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("Historical evidence is append-only.");
            if (entry.Entity is IStableReference && entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Reference data must be soft-deleted; stable codes cannot be reused.");
            if (entry.Entity is IStableReference && entry.State == EntityState.Modified &&
                entry.Properties.Any(p => p.IsModified && (p.Metadata.Name == "Code" || p.Metadata.Name.EndsWith("Id"))))
                throw new InvalidOperationException("Taxonomy codes and hierarchy are immutable.");

            // Finalized exam attempts and their persisted aggregates are immutable.
            // This is deliberately enforced below the HTTP/application layer as a
            // defense against future handlers accidentally bypassing the lifecycle.
            if (entry.Metadata.ClrType.Name == "StudentExamAttempt")
            {
                attemptType = entry.Metadata.ClrType;
                var attemptId = (Guid)entry.Property("Id").CurrentValue!;
                if (entry.State == EntityState.Added)
                    addedAttemptIds.Add(attemptId);
                else if (entry.State is EntityState.Modified or EntityState.Deleted)
                    changedResultAttemptIds.Add(attemptId);
            }

            if (entry.Metadata.ClrType.Name is "StudentExamSubjectResult" or "StudentExamTopicResult" &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var attemptProperty = entry.Property("StudentExamAttemptId");
                if (attemptProperty.CurrentValue is Guid currentAttemptId)
                    changedResultAttemptIds.Add(currentAttemptId);
                if (entry.State is EntityState.Modified or EntityState.Deleted &&
                    attemptProperty.OriginalValue is Guid originalAttemptId)
                    changedResultAttemptIds.Add(originalAttemptId);
            }
        }

        if (changedResultAttemptIds.Count == 0)
            return new EvidenceProtectionPlan(null, []);

        attemptType ??= Model.GetEntityTypes().FirstOrDefault(x => x.ClrType.Name == "StudentExamAttempt")?.ClrType
            ?? throw new InvalidOperationException("StudentExamAttempt metadata is unavailable; result immutability cannot be verified.");
        return new EvidenceProtectionPlan(attemptType,
            changedResultAttemptIds.Where(id => !addedAttemptIds.Contains(id)).ToArray());
    }

    private void ProtectEvidence()
    {
        var plan = PrepareEvidenceProtection();
        var persistedStatuses = plan.UnresolvedAttemptIds.Length == 0
            ? new Dictionary<Guid, int>()
            : LoadAttemptStatuses(plan.AttemptType!, plan.UnresolvedAttemptIds);
        ValidateChangedResults(plan, persistedStatuses);
    }

    private async Task ProtectEvidenceAsync(CancellationToken cancellationToken)
    {
        var plan = PrepareEvidenceProtection();
        var persistedStatuses = plan.UnresolvedAttemptIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await LoadAttemptStatusesAsync(plan.AttemptType!, plan.UnresolvedAttemptIds, cancellationToken);
        ValidateChangedResults(plan, persistedStatuses);
    }

    private static void ValidateChangedResults(EvidenceProtectionPlan plan, IReadOnlyDictionary<Guid, int> persistedStatuses)
    {
        foreach (var attemptId in plan.UnresolvedAttemptIds)
        {
            if (!persistedStatuses.TryGetValue(attemptId, out var status))
                throw new InvalidOperationException("The parent exam attempt could not be verified; result changes are rejected.");
            if (status == 1)
                throw new ConflictException("Results of a finalized exam attempt are immutable.");
        }

    }

    private Dictionary<Guid, int> LoadAttemptStatuses(Type attemptType, Guid[] attemptIds)
        => ReadAttemptStatuses(BuildAttemptStatusQuery(attemptType, attemptIds));

    private async Task<Dictionary<Guid, int>> LoadAttemptStatusesAsync(Type attemptType, Guid[] attemptIds,
        CancellationToken cancellationToken)
    {
        var query = BuildAttemptStatusQuery(attemptType, attemptIds);
        var toListAsync = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .Single(method => method.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync) &&
                              method.IsGenericMethodDefinition && method.GetParameters().Length == 2);
        var task = (Task)toListAsync.MakeGenericMethod(attemptType)
            .Invoke(null, [query, cancellationToken])!;
        await task.ConfigureAwait(false);
        var rows = (IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!;
        return ReadAttemptStatuses(rows);
    }

    private IQueryable BuildAttemptStatusQuery(Type attemptType, Guid[] attemptIds)
    {
        var setMethod = typeof(DbContext).GetMethods()
            .Single(method => method.Name == nameof(Set) && method.IsGenericMethodDefinition &&
                              method.GetParameters().Length == 0);
        var set = (IQueryable)setMethod.MakeGenericMethod(attemptType).Invoke(this, null)!;
        var asNoTracking = typeof(EntityFrameworkQueryableExtensions).GetMethods()
            .Single(method => method.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking) &&
                              method.IsGenericMethodDefinition && method.GetParameters().Length == 1)
            .MakeGenericMethod(attemptType);
        set = (IQueryable)asNoTracking.Invoke(null, [set])!;
        var parameter = Expression.Parameter(attemptType, "attempt");
        var id = Expression.Property(parameter, "Id");
        var contains = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(Guid)],
            Expression.Constant(attemptIds), id);
        var predicate = Expression.Lambda(contains, parameter);
        var where = Expression.Call(typeof(Queryable), nameof(Queryable.Where), [attemptType],
            set.Expression, Expression.Quote(predicate));
        return set.Provider.CreateQuery(where);
    }

    private static Dictionary<Guid, int> ReadAttemptStatuses(IEnumerable rows)
    {
        var result = new Dictionary<Guid, int>();
        foreach (var row in rows)
        {
            var type = row!.GetType();
            result[(Guid)type.GetProperty("Id")!.GetValue(row)!] =
                Convert.ToInt32(type.GetProperty("Status")!.GetValue(row));
        }
        return result;
    }

    private sealed record EvidenceProtectionPlan(Type? AttemptType, Guid[] UnresolvedAttemptIds);

    private void StampAuditFields()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
            else if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = now;
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectEvidence();
        StampAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await ProtectEvidenceAsync(cancellationToken);
        StampAuditFields();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await ProtectEvidenceAsync(cancellationToken);
        StampAuditFields();
        return await base.SaveChangesAsync(true, cancellationToken);
    }
}
