using FluentValidation;
namespace EgitimPlatform.Modules.Students.Features.ManageParents;
public record CreateParentCommand(Guid InstitutionId, Guid? UserId, string FirstName, string LastName, string? Phone = null, string? Email = null);
public record LinkParentCommand(Guid StudentId, Guid ParentId, string RelationshipType);
public record SetParentRelationshipCommand(Guid RelationshipId, bool IsActive);
public record ParentDto(Guid Id, string FirstName, string LastName);
public record StudentParentDto(Guid Id, Guid StudentId, Guid ParentId, string RelationshipType, bool IsActive);
public class CreateParentValidator : AbstractValidator<CreateParentCommand>
{
    public CreateParentValidator()
    {
        RuleFor(x => x.InstitutionId).NotEmpty();
        RuleFor(x => x.UserId).NotEqual(Guid.Empty).When(x => x.UserId.HasValue);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => x.Email is not null);
    }
}
public class LinkParentValidator : AbstractValidator<LinkParentCommand>
{
    public LinkParentValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty(); RuleFor(x => x.ParentId).NotEmpty();
        RuleFor(x => x.RelationshipType).Must(x => new[] { "Mother", "Father", "Guardian" }.Contains(x));
    }
}
public class SetParentRelationshipValidator : AbstractValidator<SetParentRelationshipCommand>
{
    public SetParentRelationshipValidator() => RuleFor(x => x.RelationshipId).NotEmpty();
}
