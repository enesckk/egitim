using EgitimPlatform.Modules.Teachers.Features.ManageClassrooms;
using FluentAssertions;
using Xunit;

namespace EgitimPlatform.Unit.Validators;

public class ClassroomValidatorTests
{
    private readonly CreateClassroomValidator _createValidator = new();
    private readonly AssignClassroomTeacherValidator _assignTeacherValidator = new();
    private readonly AssignClassroomStudentValidator _assignStudentValidator = new();

    [Fact]
    public void CreateClassroom_ValidCommand_ShouldPass()
    {
        var command = new CreateClassroomCommand(
            InstitutionId: Guid.NewGuid(),
            Name: "12-A Sayısal",
            Branch: "Kadıköy",
            GradeLevel: 12,
            AcademicYear: "2026-2027");

        var result = _createValidator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateClassroom_EmptyName_ShouldFail()
    {
        var command = new CreateClassroomCommand(
            InstitutionId: Guid.NewGuid(),
            Name: "",
            Branch: "Kadıköy");

        var result = _createValidator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(-1)]
    public void CreateClassroom_InvalidGradeLevel_ShouldFail(int grade)
    {
        var command = new CreateClassroomCommand(
            InstitutionId: Guid.NewGuid(),
            Name: "12-A",
            GradeLevel: grade);

        var result = _createValidator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "GradeLevel");
    }

    [Fact]
    public void AssignTeacher_EmptyIds_ShouldFail()
    {
        var command = new AssignClassroomTeacherCommand(Guid.Empty, Guid.Empty);
        var result = _assignTeacherValidator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClassroomId");
        result.Errors.Should().Contain(e => e.PropertyName == "TeacherId");
    }

    [Fact]
    public void AssignStudent_EmptyIds_ShouldFail()
    {
        var command = new AssignClassroomStudentCommand(Guid.Empty, Guid.Empty);
        var result = _assignStudentValidator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClassroomId");
        result.Errors.Should().Contain(e => e.PropertyName == "StudentId");
    }
}
