using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Questions.Features.CreateQuestion;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Questions.Features.UpdateQuestion;

public record UpdateQuestionCommand(
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
    QuestionStatus Status
);

public class UpdateQuestionCommandValidator : AbstractValidator<UpdateQuestionCommand>
{
    private static readonly string[] ValidLabels = ["A", "B", "C", "D", "E"];

    public UpdateQuestionCommandValidator()
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

public class UpdateQuestionHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAcademicCatalog academicCatalog,
    IAuditService auditService,
    IQuestionUsageChecker questionUsageChecker)
{
    public async Task HandleAsync(Guid questionId, UpdateQuestionCommand command, CancellationToken ct = default)
    {
        await new UpdateQuestionCommandValidator().ValidateAndThrowAsync(command, ct);

        var question = await db.Set<Question>()
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            throw new NotFoundException($"Question with ID '{questionId}' was not found.");

        // Security check: Global vs Tenant
        if (question.InstitutionId == null)
        {
            if (!currentUser.IsSuperAdmin)
                throw new ForbiddenException("Only SuperAdmin can edit global pool questions.");
        }
        else
        {
            var userInstId = await currentUser.GetInstitutionIdAsync();
            if (!userInstId.HasValue || userInstId.Value != question.InstitutionId.Value)
            {
                throw new NotFoundException($"Question with ID '{questionId}' was not found.");
            }
        }

        // Check if question is linked to any active ExamQuestion
        var isUsedInExams = await questionUsageChecker.IsQuestionLinkedToExamAsync(questionId, ct);

        // Historical Immutability Rule: If Published or linked to an exam, question content/options cannot be mutated.
        if (isUsedInExams || question.Status == QuestionStatus.Published)
        {
            throw new ConflictException("This question is published or linked to an exam and is immutable. Create a new question version instead.");
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

        question.SubjectId = command.SubjectId;
        question.TopicId = command.TopicId;
        question.SubTopicId = command.SubTopicId;
        question.LearningOutcomeId = command.LearningOutcomeId;
        question.Difficulty = command.Difficulty;
        question.SourceType = command.SourceType;
        question.SourceYear = command.SourceYear;
        question.Content = command.Content.Trim();
        question.MediaUrl = command.MediaUrl?.Trim();
        question.Status = command.Status;

        // Replace options
        question.Options.Clear();
        question.Options.AddRange(command.Options.Select(o => new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            OptionLabel = o.OptionLabel.Trim().ToUpperInvariant(),
            Content = o.Content.Trim(),
            IsCorrect = o.IsCorrect
        }));

        if (currentUser.UserId.HasValue)
        {
            await auditService.LogAsync(currentUser.UserId.Value, "QuestionUpdated", "Question", question.Id.ToString(), question.InstitutionId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
