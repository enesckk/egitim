using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.SaveAnswers;

public record RawAnswerDto(
    Guid ExamQuestionId,
    string? SelectedOption
);

public record SaveAnswersCommand(
    List<RawAnswerDto> Answers
);

public class SaveAnswersCommandValidator : AbstractValidator<SaveAnswersCommand>
{
    private static readonly string[] ValidOptionLabels = ["A", "B", "C", "D", "E"];

    public SaveAnswersCommandValidator()
    {
        RuleFor(x => x.Answers)
            .NotNull()
            .WithMessage("Answers collection cannot be null.");

        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.ExamQuestionId).NotEmpty();
            answer.When(a => !string.IsNullOrWhiteSpace(a.SelectedOption), () =>
            {
                answer.RuleFor(a => a.SelectedOption!)
                    .Must(opt => ValidOptionLabels.Contains(opt.Trim().ToUpperInvariant()))
                    .WithMessage("SelectedOption must be one of A, B, C, D, E or null for blank.");
            });
        });
    }
}

public class SaveAnswersHandler(
    IApplicationDbContext db,
    IExamResourceAccess resourceAccess,
    IAuditService auditService,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(Guid attemptId, SaveAnswersCommand command, CancellationToken ct = default)
    {
        await new SaveAnswersCommandValidator().ValidateAndThrowAsync(command, ct);

        var attempt = await resourceAccess.ValidateAttemptWriteAccessAsync(attemptId, ct);

        if (attempt.Status == AttemptStatus.Finalized)
            throw new ConflictException("Attempt is already finalized.");

        var distinctQuestionIds = command.Answers.Select(a => a.ExamQuestionId).Distinct().ToList();
        if (distinctQuestionIds.Count != command.Answers.Count)
            throw new ValidationException("Duplicate examQuestionId found in answers payload.");

        // Fetch valid ExamQuestions for this Exam
        var validExamQuestions = await db.Set<ExamQuestion>()
            .AsNoTracking()
            .Where(eq => eq.ExamId == attempt.ExamId)
            .ToDictionaryAsync(eq => eq.Id, ct);

        foreach (var answer in command.Answers)
        {
            if (!validExamQuestions.ContainsKey(answer.ExamQuestionId))
            {
                throw new NotFoundException($"ExamQuestion with ID '{answer.ExamQuestionId}' was not found in Exam '{attempt.ExamId}'.");
            }
        }

        // Fetch existing answer results for this attempt
        var existingResults = await db.Set<StudentExamQuestionResult>()
            .Where(r => r.StudentExamAttemptId == attemptId)
            .ToDictionaryAsync(r => r.ExamQuestionId, ct);

        foreach (var answer in command.Answers)
        {
            var eq = validExamQuestions[answer.ExamQuestionId];
            string? normalizedOption = string.IsNullOrWhiteSpace(answer.SelectedOption)
                ? null
                : answer.SelectedOption.Trim().ToUpperInvariant();

            if (existingResults.TryGetValue(answer.ExamQuestionId, out var existing))
            {
                existing.SelectedOption = normalizedOption;
                existing.IsBlank = normalizedOption == null;
                existing.IsCorrect = null;
                existing.NetContribution = 0m;
            }
            else
            {
                var newResult = new StudentExamQuestionResult
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = attempt.InstitutionId,
                    StudentExamAttemptId = attemptId,
                    ExamQuestionId = answer.ExamQuestionId,
                    QuestionId = eq.QuestionId,
                    SelectedOption = normalizedOption,
                    IsBlank = normalizedOption == null,
                    IsCorrect = null,
                    NetContribution = 0m
                };
                db.Set<StudentExamQuestionResult>().Add(newResult);
            }
        }

        if (currentUser.UserId.HasValue)
        {
            await auditService.LogAsync(currentUser.UserId.Value, "ExamAttemptAnswersSaved", "StudentExamAttempt", attempt.Id.ToString(), attempt.InstitutionId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
