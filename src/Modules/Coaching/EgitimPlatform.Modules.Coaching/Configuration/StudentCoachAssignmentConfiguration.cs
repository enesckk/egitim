using EgitimPlatform.Modules.Coaching.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EgitimPlatform.Modules.Coaching.Configuration;

public class StudentCoachAssignmentConfiguration : IEntityTypeConfiguration<StudentCoachAssignment>
{
    public void Configure(EntityTypeBuilder<StudentCoachAssignment> builder)
    {
        builder.ToTable("StudentCoachAssignments");

        // Useful indexes (the filtered unique for active primary is applied in ApplicationDbContext)
        builder.HasIndex(a => a.InstitutionId);
        builder.HasIndex(a => new { a.StudentId, a.IsActive });
        builder.HasIndex(a => new { a.CoachId, a.IsActive });
        builder.HasIndex(a => a.AssignedAt);

        // Note: FK constraints are applied in ApplicationDbContext via reflection
        // to avoid circular module references.
    }
}
