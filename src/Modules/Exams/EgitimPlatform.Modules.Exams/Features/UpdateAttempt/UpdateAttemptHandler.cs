using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Features.CreateAttempt;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.UpdateAttempt;

public record UpdateAttemptCommand(
    DateTimeOffset TakenAt,
    decimal? ReportedScore,
    ScoreSource ScoreSource,
    byte[] RowVersion,
    List<CreateSectionResultDto> Sections
);

public class UpdateAttemptCommandValidator : AbstractValidator<UpdateAttemptCommand>
{
    public UpdateAttemptCommandValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion concurrency token is required.");
        RuleFor(x => x.Sections).NotEmpty().WithMessage("Attempt must contain results for exam sections.");

        RuleForEach(x => x.Sections).ChildRules(s =>
        {
            s.RuleFor(x => x.ExamSectionId).NotEmpty();
            s.RuleFor(x => x.Correct).GreaterThanOrEqualTo(0);
            s.RuleFor(x => x.Wrong).GreaterThanOrEqualTo(0);
            s.RuleFor(x => x.Blank).GreaterThanOrEqualTo(0);

            s.RuleForEach(x => x.Topics).ChildRules(t =>
            {
                t.RuleFor(x => x.TopicId).NotEmpty();
                t.RuleFor(x => x.Correct).GreaterThanOrEqualTo(0);
                t.RuleFor(x => x.Wrong).GreaterThanOrEqualTo(0);
                t.RuleFor(x => x.Blank).GreaterThanOrEqualTo(0);
            });
        });
    }
}

public class UpdateAttemptHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IExamResourceAccess resourceAccess,
    IExamScoringService scoringService,
    IAcademicCatalog academicCatalog,
    IAuditService auditService)
{
    public async Task HandleAsync(Guid attemptId, UpdateAttemptCommand command, CancellationToken ct = default)
    {
        await new UpdateAttemptCommandValidator().ValidateAndThrowAsync(command, ct);

        var attempt = await resourceAccess.ValidateAttemptWriteAccessAsync(attemptId, ct);

        // Immutability check: Only Draft attempts can be updated
        if (attempt.Status == AttemptStatus.Finalized)
            throw new ConflictException("Finalized exam attempts are immutable and cannot be updated.");

        // Concurrency check
        if (attempt.RowVersion is not null && !attempt.RowVersion.SequenceEqual(command.RowVersion))
            throw new ConflictException("The record was modified by another user. Please reload and try again.");

        var exam = await db.Set<Exam>().AsNoTracking().SingleAsync(e => e.Id == attempt.ExamId, ct);

        var dbSections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == attempt.ExamId)
            .ToDictionaryAsync(s => s.Id, ct);

        // Check duplicate section submissions
        var sectionInputIds = command.Sections.Select(s => s.ExamSectionId).ToList();
        if (sectionInputIds.Distinct().Count() != sectionInputIds.Count)
            throw new ValidationException("Duplicate section result submissions are not allowed.");

        foreach (var secInput in command.Sections)
        {
            if (!dbSections.TryGetValue(secInput.ExamSectionId, out var dbSec))
                throw new ValidationException($"ExamSection '{secInput.ExamSectionId}' does not belong to Exam '{attempt.ExamId}'.");

            if (secInput.Correct + secInput.Wrong + secInput.Blank != dbSec.QuestionCount)
                throw new ValidationException(
                    $"Section '{dbSec.Id}' total (Correct: {secInput.Correct} + Wrong: {secInput.Wrong} + Blank: {secInput.Blank}) must equal QuestionCount ({dbSec.QuestionCount}).");

            if (secInput.Topics is { Count: > 0 })
            {
                var topicIds = secInput.Topics.Select(t => t.TopicId).ToList();
                if (topicIds.Distinct().Count() != topicIds.Count)
                    throw new ValidationException($"Duplicate topic results in section '{dbSec.Id}'.");

                var topicCorrectSum = secInput.Topics.Sum(t => t.Correct);
                var topicWrongSum = secInput.Topics.Sum(t => t.Wrong);
                var topicBlankSum = secInput.Topics.Sum(t => t.Blank);

                if (topicCorrectSum > secInput.Correct)
                    throw new ValidationException($"Topic correct sum ({topicCorrectSum}) cannot exceed section correct ({secInput.Correct}).");
                if (topicWrongSum > secInput.Wrong)
                    throw new ValidationException($"Topic wrong sum ({topicWrongSum}) cannot exceed section wrong ({secInput.Wrong}).");
                if (topicBlankSum > secInput.Blank)
                    throw new ValidationException($"Topic blank sum ({topicBlankSum}) cannot exceed section blank ({secInput.Blank}).");

                var validTopics = await academicCatalog.ValidateSubjectTopicsAsync(dbSec.SubjectId, topicIds, ct);
                if (!validTopics)
                    throw new ValidationException($"One or more topics do not belong to Subject '{dbSec.SubjectId}'.");
            }
        }

        // Remove old child results
        var existingSubjectResults = await db.Set<StudentExamSubjectResult>()
            .Where(r => r.StudentExamAttemptId == attemptId)
            .ToListAsync(ct);
        var existingTopicResults = await db.Set<StudentExamTopicResult>()
            .Where(r => r.StudentExamAttemptId == attemptId)
            .ToListAsync(ct);

        foreach (var sr in existingSubjectResults) db.Set<StudentExamSubjectResult>().Remove(sr);
        foreach (var tr in existingTopicResults) db.Set<StudentExamTopicResult>().Remove(tr);

        int totalCorrect = 0, totalWrong = 0, totalBlank = 0;
        decimal totalNet = 0m;

        foreach (var secInput in command.Sections)
        {
            var dbSec = dbSections[secInput.ExamSectionId];
            var penalty = dbSec.WrongAnswerPenalty ?? exam.WrongAnswerPenalty;

            var secNet = scoringService.CalculateNet(secInput.Correct, secInput.Wrong, penalty);

            totalCorrect += secInput.Correct;
            totalWrong += secInput.Wrong;
            totalBlank += secInput.Blank;
            totalNet += secNet;

            db.Set<StudentExamSubjectResult>().Add(new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = attempt.InstitutionId,
                StudentExamAttemptId = attemptId,
                ExamSectionId = dbSec.Id,
                Correct = secInput.Correct,
                Wrong = secInput.Wrong,
                Blank = secInput.Blank,
                Net = secNet
            });

            if (secInput.Topics is { Count: > 0 })
            {
                foreach (var topInput in secInput.Topics)
                {
                    var topNet = scoringService.CalculateNet(topInput.Correct, topInput.Wrong, penalty);
                    db.Set<StudentExamTopicResult>().Add(new StudentExamTopicResult
                    {
                        Id = Guid.NewGuid(),
                        InstitutionId = attempt.InstitutionId,
                        StudentExamAttemptId = attemptId,
                        ExamSectionId = dbSec.Id,
                        TopicId = topInput.TopicId,
                        Correct = topInput.Correct,
                        Wrong = topInput.Wrong,
                        Blank = topInput.Blank,
                        Net = topNet
                    });
                }
            }
        }

        attempt.TakenAt = command.TakenAt;
        attempt.ReportedScore = command.ReportedScore;
        attempt.ScoreSource = command.ScoreSource;
        attempt.TotalCorrect = totalCorrect;
        attempt.TotalWrong = totalWrong;
        attempt.TotalBlank = totalBlank;
        attempt.TotalNet = totalNet;

        if (currentUser.UserId.HasValue)
        {
            await auditService.LogAsync(currentUser.UserId.Value, "ExamAttemptUpdated", "StudentExamAttempt", attempt.Id.ToString(), attempt.InstitutionId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
