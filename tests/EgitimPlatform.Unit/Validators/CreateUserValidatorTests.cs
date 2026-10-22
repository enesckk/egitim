using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.Modules.Identity.Features.Users;
using FluentAssertions;
using Xunit;

namespace EgitimPlatform.Unit.Validators;

public class CreateUserValidatorTests
{
    private readonly CreateUserValidator _validator = new();

    [Theory]
    [InlineData(Roles.Student)]
    [InlineData(Roles.Teacher)]
    [InlineData(Roles.Coach)]
    [InlineData(Roles.Parent)]
    public void ValidCommand_AllowedRoles_ShouldPass(string role)
    {
        var command = new CreateUserCommand(
            Email: "user@test.local",
            Password: "SecurePassword123!",
            FirstName: "Ahmet",
            LastName: "Yılmaz",
            Role: role);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvalidEmail_ShouldFail()
    {
        var command = new CreateUserCommand(
            Email: "not-an-email",
            Password: "SecurePassword123!",
            FirstName: "Ahmet",
            LastName: "Yılmaz",
            Role: Roles.Student);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void ShortPassword_ShouldFail()
    {
        var command = new CreateUserCommand(
            Email: "user@test.local",
            Password: "123",
            FirstName: "Ahmet",
            LastName: "Yılmaz",
            Role: Roles.Student);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void EmptyName_ShouldFail()
    {
        var command = new CreateUserCommand(
            Email: "user@test.local",
            Password: "SecurePassword123!",
            FirstName: "",
            LastName: "Yılmaz",
            Role: Roles.Student);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void InvalidRole_ShouldFail()
    {
        var command = new CreateUserCommand(
            Email: "user@test.local",
            Password: "SecurePassword123!",
            FirstName: "Ahmet",
            LastName: "Yılmaz",
            Role: "HackerRole");

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role");
    }
}
