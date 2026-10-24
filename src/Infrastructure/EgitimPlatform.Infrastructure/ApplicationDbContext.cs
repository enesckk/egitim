using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Identity.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

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
            "EgitimPlatform.Modules.Questions",
            "EgitimPlatform.Modules.Planning",
            "EgitimPlatform.Modules.Communication",
            "EgitimPlatform.Modules.Notebook",
            "EgitimPlatform.Modules.Content",
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
        if (goalType is not null && examType is not null)
            builder.Entity(goalType.ClrType).HasOne(examType.ClrType).WithMany()
                .HasForeignKey("TargetExamTypeId").OnDelete(DeleteBehavior.Restrict);
        if (teacherSubjectType is not null && subjectType is not null)
            builder.Entity(teacherSubjectType.ClrType).HasOne(subjectType.ClrType).WithMany()
                .HasForeignKey("SubjectId").OnDelete(DeleteBehavior.Restrict);

        // Phase 2: Classroom & ClassroomStudent cross-module FKs
        var classroomType = FindEntityType(builder, "Classroom");
        var classroomStudentType = FindEntityType(builder, "ClassroomStudent");
        var classroomTeacherType = FindEntityType(builder, "ClassroomTeacher");

        if (classroomType is not null && institutionType is not null)
        {
            builder.Entity(classroomType.ClrType)
                .HasOne(institutionType.ClrType)
                .WithMany()
                .HasForeignKey("InstitutionId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        if (classroomTeacherType is not null && institutionType is not null)
        {
            builder.Entity(classroomTeacherType.ClrType)
                .HasOne(institutionType.ClrType)
                .WithMany()
                .HasForeignKey("InstitutionId")
                .OnDelete(DeleteBehavior.Restrict);
        }

        if (classroomStudentType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(classroomStudentType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (studentType is not null)
            {
                builder.Entity(classroomStudentType.ClrType)
                    .HasOne(studentType.ClrType)
                    .WithMany()
                    .HasForeignKey("StudentId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
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

        // Sprint 3: Exams module cross-module & composite tenant-safe FKs
        var examEntityType = FindEntityType(builder, "Exam");
        var examSectionType = FindEntityType(builder, "ExamSection");
        var attemptType = FindEntityType(builder, "StudentExamAttempt");
        var subjectResultType = FindEntityType(builder, "StudentExamSubjectResult");
        var topicResultType = FindEntityType(builder, "StudentExamTopicResult");
        var topicType = FindEntityType(builder, "Topic");

        if (examEntityType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(examEntityType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (examType is not null)
            {
                builder.Entity(examEntityType.ClrType)
                    .HasOne(examType.ClrType)
                    .WithMany()
                    .HasForeignKey("ExamTypeId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        if (examSectionType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(examSectionType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (subjectType is not null)
            {
                builder.Entity(examSectionType.ClrType)
                    .HasOne(subjectType.ClrType)
                    .WithMany()
                    .HasForeignKey("SubjectId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (examEntityType is not null)
            {
                builder.Entity(examSectionType.ClrType)
                    .HasOne(examEntityType.ClrType)
                    .WithMany("Sections")
                    .HasForeignKey("ExamId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        if (attemptType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(attemptType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (examEntityType is not null)
            {
                builder.Entity(attemptType.ClrType)
                    .HasOne(examEntityType.ClrType)
                    .WithMany()
                    .HasForeignKey("ExamId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (studentType is not null)
            {
                builder.Entity(attemptType.ClrType)
                    .HasOne(studentType.ClrType)
                    .WithMany()
                    .HasForeignKey("StudentId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        if (subjectResultType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(subjectResultType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (attemptType is not null)
            {
                builder.Entity(subjectResultType.ClrType)
                    .HasOne(attemptType.ClrType)
                    .WithMany("SubjectResults")
                    .HasForeignKey("StudentExamAttemptId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (examSectionType is not null)
            {
                builder.Entity(subjectResultType.ClrType)
                    .HasOne(examSectionType.ClrType)
                    .WithMany()
                    .HasForeignKey("ExamSectionId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        if (topicResultType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(topicResultType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (attemptType is not null)
            {
                builder.Entity(topicResultType.ClrType)
                    .HasOne(attemptType.ClrType)
                    .WithMany("TopicResults")
                    .HasForeignKey("StudentExamAttemptId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (examSectionType is not null)
            {
                builder.Entity(topicResultType.ClrType)
                    .HasOne(examSectionType.ClrType)
                    .WithMany()
                    .HasForeignKey("ExamSectionId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (topicType is not null)
            {
                builder.Entity(topicResultType.ClrType)
                    .HasOne(topicType.ClrType)
                    .WithMany()
                    .HasForeignKey("TopicId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        // Sprint 4A: Question, ExamQuestion, and StudentExamQuestionResult cross-module & composite tenant-safe FKs
        var questionType = FindEntityType(builder, "Question");
        var examQuestionType = FindEntityType(builder, "ExamQuestion");
        var questionResultType = FindEntityType(builder, "StudentExamQuestionResult");
        var subTopicType = FindEntityType(builder, "SubTopic");
        var learningOutcomeType = FindEntityType(builder, "LearningOutcome");

        if (questionType is not null)
        {
            if (subjectType is not null)
            {
                builder.Entity(questionType.ClrType)
                    .HasOne(subjectType.ClrType)
                    .WithMany()
                    .HasForeignKey("SubjectId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (topicType is not null)
            {
                builder.Entity(questionType.ClrType)
                    .HasOne(topicType.ClrType)
                    .WithMany()
                    .HasForeignKey("TopicId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (subTopicType is not null)
            {
                builder.Entity(questionType.ClrType)
                    .HasOne(subTopicType.ClrType)
                    .WithMany()
                    .HasForeignKey("SubTopicId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (learningOutcomeType is not null)
            {
                builder.Entity(questionType.ClrType)
                    .HasOne(learningOutcomeType.ClrType)
                    .WithMany()
                    .HasForeignKey("LearningOutcomeId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        if (examQuestionType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(examQuestionType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (examEntityType is not null)
            {
                builder.Entity(examQuestionType.ClrType)
                    .HasOne(examEntityType.ClrType)
                    .WithMany()
                    .HasForeignKey("ExamId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (examSectionType is not null)
            {
                builder.Entity(examQuestionType.ClrType)
                    .HasOne(examSectionType.ClrType)
                    .WithMany()
                    .HasForeignKey("ExamSectionId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (questionType is not null)
            {
                builder.Entity(examQuestionType.ClrType)
                    .HasOne(questionType.ClrType)
                    .WithMany()
                    .HasForeignKey("QuestionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        if (questionResultType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(questionResultType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (attemptType is not null)
            {
                builder.Entity(questionResultType.ClrType)
                    .HasOne(attemptType.ClrType)
                    .WithMany("QuestionResults")
                    .HasForeignKey("StudentExamAttemptId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (examQuestionType is not null)
            {
                builder.Entity(questionResultType.ClrType)
                    .HasOne(examQuestionType.ClrType)
                    .WithMany()
                    .HasForeignKey("ExamQuestionId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (questionType is not null)
            {
                builder.Entity(questionResultType.ClrType)
                    .HasOne(questionType.ClrType)
                    .WithMany()
                    .HasForeignKey("QuestionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        var coachTaskType = FindEntityType(builder, "CoachTask");
        if (coachTaskType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(coachTaskType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (studentType is not null)
            {
                builder.Entity(coachTaskType.ClrType)
                    .HasOne(studentType.ClrType)
                    .WithMany()
                    .HasForeignKey("StudentId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        var coachPrivateNoteType = FindEntityType(builder, "CoachPrivateNote");
        if (coachPrivateNoteType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(coachPrivateNoteType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (studentType is not null)
            {
                builder.Entity(coachPrivateNoteType.ClrType)
                    .HasOne(studentType.ClrType)
                    .WithMany()
                    .HasForeignKey("StudentId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        var notebookEntryType = FindEntityType(builder, "NotebookEntry");
        if (notebookEntryType is not null)
        {
            if (institutionType is not null)
            {
                builder.Entity(notebookEntryType.ClrType)
                    .HasOne(institutionType.ClrType)
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }

            if (studentType is not null)
            {
                builder.Entity(notebookEntryType.ClrType)
                    .HasOne(studentType.ClrType)
                    .WithMany()
                    .HasForeignKey("StudentId", "InstitutionId")
                    .HasPrincipalKey("Id", "InstitutionId")
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }
    }

    private static IMutableEntityType? FindEntityType(ModelBuilder builder, string typeName)
    {
        return builder.Model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.Name == typeName);
    }

    private void ProtectEvidence()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IImmutableHistory && entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("Historical evidence is append-only.");
            if (entry.Entity is IStableReference && entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Reference data must be soft-deleted; stable codes cannot be reused.");
            if (entry.Entity is IStableReference && entry.State == EntityState.Modified &&
                entry.Properties.Any(p => p.IsModified && (p.Metadata.Name == "Code" || p.Metadata.Name.EndsWith("Id"))))
                throw new InvalidOperationException("Taxonomy codes and hierarchy are immutable.");
        }
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectEvidence();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ProtectEvidence();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ProtectEvidence();
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

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => Database.BeginTransactionAsync(cancellationToken);
}
