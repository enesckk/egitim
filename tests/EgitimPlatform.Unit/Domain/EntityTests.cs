using EgitimPlatform.BuildingBlocks.Entities;
using EgitimPlatform.Modules.Coaching.Entities;
using EgitimPlatform.Modules.Students.Entities;
using FluentAssertions;
using Xunit;

namespace EgitimPlatform.Unit.Domain;

public class EntityTests
{
    [Fact]
    public void Student_DefaultStatus_IsActive()
    {
        var student = new Student
        {
            InstitutionId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Student",
        };

        student.Status.Should().Be(StudentStatus.Active);
    }

    [Fact]
    public void SoftDeletableEntity_DefaultNotDeleted()
    {
        var student = new Student
        {
            InstitutionId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Student",
        };

        student.IsDeleted.Should().BeFalse();
        student.DeletedAt.Should().BeNull();
        student.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void StudentCoachAssignment_DefaultIsActive()
    {
        var assignment = new StudentCoachAssignment
        {
            StudentId = Guid.NewGuid(),
            CoachId = Guid.NewGuid(),
            InstitutionId = Guid.NewGuid(),
        };

        assignment.IsActive.Should().BeTrue();
        assignment.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void BaseEntity_HasId()
    {
        var student = new Student
        {
            InstitutionId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Student",
        };

        student.Id.Should().NotBeEmpty();
    }
}
