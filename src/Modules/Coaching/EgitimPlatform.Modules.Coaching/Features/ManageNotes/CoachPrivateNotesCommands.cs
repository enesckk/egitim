using FluentValidation;

namespace EgitimPlatform.Modules.Coaching.Features.ManageNotes;

public record CoachPrivateNoteDto(
    Guid Id,
    Guid StudentId,
    Guid CoachId,
    string Category,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateCoachPrivateNoteCommand(
    Guid StudentId,
    string Category,
    string Content);

public record UpdateCoachPrivateNoteCommand(
    Guid NoteId,
    string Category,
    string Content);

public record DeleteCoachPrivateNoteCommand(
    Guid NoteId);

public record ListCoachPrivateNotesQuery(
    Guid StudentId);

public record CreateCoachPrivateNoteRequest(
    string Category,
    string Content);

public record UpdateCoachPrivateNoteRequest(
    string Category,
    string Content);

public class CreateCoachPrivateNoteValidator : AbstractValidator<CreateCoachPrivateNoteCommand>
{
    private static readonly string[] ValidCategories = ["Akademik", "Motivasyon", "Veli Görüşmesi", "Genel"];

    public CreateCoachPrivateNoteValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Category).NotEmpty()
            .Must(c => ValidCategories.Contains(c))
            .WithMessage("Kategori şunlardan biri olmalıdır: Akademik, Motivasyon, Veli Görüşmesi, Genel.");
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}

public class UpdateCoachPrivateNoteValidator : AbstractValidator<UpdateCoachPrivateNoteCommand>
{
    private static readonly string[] ValidCategories = ["Akademik", "Motivasyon", "Veli Görüşmesi", "Genel"];

    public UpdateCoachPrivateNoteValidator()
    {
        RuleFor(x => x.NoteId).NotEmpty();
        RuleFor(x => x.Category).NotEmpty()
            .Must(c => ValidCategories.Contains(c))
            .WithMessage("Kategori şunlardan biri olmalıdır: Akademik, Motivasyon, Veli Görüşmesi, Genel.");
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}
