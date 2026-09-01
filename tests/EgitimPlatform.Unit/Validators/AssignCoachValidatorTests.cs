using EgitimPlatform.Modules.Coaching.Features.AssignCoach;
using FluentAssertions;
using Xunit;

namespace EgitimPlatform.Unit.Validators;

public class AssignCoachValidatorTests
{
    private readonly AssignCoachValidator _validator = new();

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var command = new AssignCoachCommand(Guid.NewGuid(), Guid.NewGuid(), true);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyStudentId_ShouldFail()
    {
        var command = new AssignCoachCommand(Guid.Empty, Guid.NewGuid(), false);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyCoachId_ShouldFail()
    {
        var command = new AssignCoachCommand(Guid.NewGuid(), Guid.Empty, false);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SameStudentAndCoachId_ShouldFail()
    {
        var id = Guid.NewGuid();
        var command = new AssignCoachCommand(id, id, false);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }
}
