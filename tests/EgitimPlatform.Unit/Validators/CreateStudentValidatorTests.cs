using EgitimPlatform.Modules.Students.Features.CreateStudent;
using FluentAssertions;
using Xunit;

namespace EgitimPlatform.Unit.Validators;

public class CreateStudentValidatorTests
{
    private readonly CreateStudentValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var command = new CreateStudentCommand("John", "Doe", null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyFirstName_ShouldFail()
    {
        var command = new CreateStudentCommand("", "Doe", null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void EmptyLastName_ShouldFail()
    {
        var command = new CreateStudentCommand("John", "", null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public void LongFirstName_ShouldFail()
    {
        var command = new CreateStudentCommand(new string('A', 101), "Doe", null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidEmail_ShouldFail()
    {
        var command = new CreateStudentCommand("John", "Doe", "not-an-email");
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void NullEmail_ShouldPass()
    {
        var command = new CreateStudentCommand("John", "Doe", null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
