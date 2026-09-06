using FluentValidation;
namespace EgitimPlatform.Modules.Academic.Features;
public record TaxonomyQuery(string Level, Guid? ParentId = null, int Page = 1, int PageSize = 100);
public class TaxonomyQueryValidator : AbstractValidator<TaxonomyQuery>
{
    public TaxonomyQueryValidator()
    {
        RuleFor(x => x.Level).Must(x => new[] { "exam-types", "subjects", "topics", "sub-topics", "learning-outcomes", "concepts" }.Contains(x));
        RuleFor(x => x.ParentId).NotEqual(Guid.Empty).When(x => x.ParentId.HasValue);
        RuleFor(x => x.Page).InclusiveBetween(1, 100000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
public record TaxonomyDto(Guid Id, string Code, string Name, Guid? ParentId);
