using FluentValidation;

namespace EgitimPlatform.Modules.Notebook.Features.ManageNotebook;

public record NotebookEntryDto(
    Guid Id,
    Guid StudentId,
    string Title,
    string Content,
    Guid? SubjectId,
    Guid? TopicId,
    string? Tags,
    bool IsPinned,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateNotebookEntryCommand(
    string Title,
    string Content,
    Guid? SubjectId = null,
    Guid? TopicId = null,
    string? Tags = null,
    bool IsPinned = false);

public record UpdateNotebookEntryRequest(
    string Title,
    string Content,
    Guid? SubjectId = null,
    Guid? TopicId = null,
    string? Tags = null,
    bool IsPinned = false);

public record UpdateNotebookEntryCommand(
    Guid Id,
    string Title,
    string Content,
    Guid? SubjectId = null,
    Guid? TopicId = null,
    string? Tags = null,
    bool IsPinned = false);

public record DeleteNotebookEntryCommand(
    Guid Id);

public record ListNotebookEntriesQuery(
    int Page = 1,
    int PageSize = 50,
    Guid? SubjectId = null,
    string? SearchTerm = null,
    bool? IsPinned = null);

public class CreateNotebookEntryValidator : AbstractValidator<CreateNotebookEntryCommand>
{
    public CreateNotebookEntryValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Tags).MaximumLength(500).When(x => x.Tags is not null);
    }
}

public class UpdateNotebookEntryValidator : AbstractValidator<UpdateNotebookEntryCommand>
{
    public UpdateNotebookEntryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty();
        RuleFor(x => x.Tags).MaximumLength(500).When(x => x.Tags is not null);
    }
}

public class ListNotebookEntriesValidator : AbstractValidator<ListNotebookEntriesQuery>
{
    public ListNotebookEntriesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
