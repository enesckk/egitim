using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Questions.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Questions.Features.CreateQuestion;

public record QuestionOptionDto(
    string OptionLabel,
    string Content,
    bool IsCorrect
);

public record CreateQuestionCommand(
    Guid SubjectId,
    Guid TopicId,
    Guid? SubTopicId,
    Guid? LearningOutcomeId,
    DifficultyLevel Difficulty,
    QuestionSourceType SourceType,
    int? SourceYear,
    string Content,
    string? MediaUrl,
    List<QuestionOptionDto> Options,
    QuestionStatus Status = QuestionStatus.Draft,
    bool IsGlobal = false
);

public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
{
    private static readonly string[] ValidLabels = ["A", "B", "C", "D", "E"];

    public CreateQuestionCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().WithMessage("Question content cannot be empty.");

        When(x => x.SourceYear.HasValue, () =>
        {
            RuleFor(x => x.SourceYear!.Value)
                .InclusiveBetween(1900, 2100)
                .WithMessage("SourceYear must be between 1900 and 2100.");
        });

        RuleFor(x => x.Options)
            .NotNull()
            .Must(opts => opts != null && opts.Count == 5)
            .WithMessage("MultipleChoice questions must contain exactly 5 options (A, B, C, D, E).");

        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.OptionLabel)
                .NotEmpty()
                .Must(label => !string.IsNullOrWhiteSpace(label) && ValidLabels.Contains(label.Trim().ToUpperInvariant()))
                .WithMessage("OptionLabel must be one of A, B, C, D, E.");

            option.RuleFor(o => o.Content).NotEmpty().WithMessage("Option content cannot be empty.");
        });

        RuleFor(x => x.Options)
            .Must(opts =>
            {
                if (opts is null || opts.Count != 5) return false;
                var normalizedLabels = opts.Select(o => o.OptionLabel.Trim().ToUpperInvariant()).ToList();
                return normalizedLabels.Distinct().Count() == 5 && normalizedLabels.All(ValidLabels.Contains);
            })
            .WithMessage("Options must contain unique labels matching A, B, C, D, E.");

        RuleFor(x => x.Options)
            .Must(opts => opts != null && opts.Count(o => o.IsCorrect) == 1)
            .WithMessage("Exactly ONE option must be marked as correct.");
    }
}

public class CreateQuestionHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAcademicCatalog academicCatalog,
    IAuditService auditService)
{
    public async Task<Guid> HandleAsync(CreateQuestionCommand command, CancellationToken ct = default)
    {
        await new CreateQuestionCommandValidator().ValidateAndThrowAsync(command, ct);

        Guid? institutionId = null;
        if (command.IsGlobal)
        {
            if (!currentUser.IsSuperAdmin)
                throw new ForbiddenException("Only SuperAdmin can create global pool questions.");
        }
        else
        {
            var userInstId = await currentUser.GetInstitutionIdAsync();
            if (!userInstId.HasValue)
                throw new ForbiddenException("Institution context required to create custom questions.");
            institutionId = userInstId.Value;
        }

        // Full Taxonomy Chain Validation
        var subjectExists = await academicCatalog.SubjectExistsAsync(command.SubjectId, ct);
        if (!subjectExists)
            throw new ValidationException($"Subject with ID '{command.SubjectId}' does not exist.");

        var topicValid = await academicCatalog.ValidateSubjectTopicsAsync(command.SubjectId, [command.TopicId], ct);
        if (!topicValid)
            throw new ValidationException($"Topic '{command.TopicId}' does not belong to Subject '{command.SubjectId}'.");

        if (command.SubTopicId.HasValue)
        {
            var subTopicValid = await academicCatalog.ValidateTopicSubTopicAsync(command.TopicId, command.SubTopicId.Value, ct);
            if (!subTopicValid)
                throw new ValidationException($"SubTopic '{command.SubTopicId.Value}' does not belong to Topic '{command.TopicId}'.");
        }

        if (command.LearningOutcomeId.HasValue)
        {
            var loValid = await academicCatalog.ValidateLearningOutcomeHierarchyAsync(command.SubjectId, command.TopicId, command.SubTopicId, command.LearningOutcomeId.Value, ct);
            if (!loValid)
                throw new ValidationException($"LearningOutcome '{command.LearningOutcomeId.Value}' does not belong to the specified taxonomy hierarchy.");
        }

        var question = new Question
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            SubjectId = command.SubjectId,
            TopicId = command.TopicId,
            SubTopicId = command.SubTopicId,
            LearningOutcomeId = command.LearningOutcomeId,
            QuestionType = QuestionType.MultipleChoice,
            Difficulty = command.Difficulty,
            SourceType = command.SourceType,
            SourceYear = command.SourceYear,
            Content = command.Content.Trim(),
            MediaUrl = command.MediaUrl?.Trim(),
            Status = command.Status,
            Options = command.Options.Select(o => new QuestionOption
            {
                Id = Guid.NewGuid(),
                OptionLabel = o.OptionLabel.Trim().ToUpperInvariant(),
                Content = o.Content.Trim(),
                IsCorrect = o.IsCorrect
            }).ToList()
        };

        db.Set<Question>().Add(question);

        if (currentUser.UserId.HasValue)
        {
            await auditService.LogAsync(currentUser.UserId.Value, "QuestionCreated", "Question", question.Id.ToString(), institutionId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);

        return question.Id;
    }
}
