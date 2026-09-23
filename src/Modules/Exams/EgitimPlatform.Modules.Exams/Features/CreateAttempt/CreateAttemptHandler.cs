using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.CreateAttempt;

public record CreateTopicResultDto(
    Guid TopicId,
    int Correct,
    int Wrong,
    int Blank
);

public record CreateSectionResultDto(
    Guid ExamSectionId,
    int Correct,
    int Wrong,
    int Blank,
    List<CreateTopicResultDto>? Topics = null
);

public record CreateAttemptCommand(
    Guid StudentId,
    DateTimeOffset TakenAt,
    int? AttemptNumber,
    decimal? ReportedScore,
    ScoreSource ScoreSource,
    List<CreateSectionResultDto> Sections
);

public class CreateAttemptCommandValidator : AbstractValidator<CreateAttemptCommand>
{
    public CreateAttemptCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
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

public class CreateAttemptHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IExamResourceAccess resourceAccess,
    IExamScoringService scoringService,
    IAcademicCatalog academicCatalog,
    IAuditService auditService)
{
    public async Task<Guid> HandleAsync(Guid examId, CreateAttemptCommand command, CancellationToken ct = default)
    {
        await new CreateAttemptCommandValidator().ValidateAndThrowAsync(command, ct);

        var exam = await resourceAccess.ValidateExamAccessAsync(examId, ct);
        var student = await resourceAccess.ValidateStudentWriteAccessAsync(command.StudentId, ct);

        // Fetch Exam Sections
        var dbSections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == examId)
            .ToDictionaryAsync(s => s.Id, ct);

        if (dbSections.Count == 0)
            throw new ValidationException("Cannot submit attempt for an exam without sections.");

        // Check duplicate section submissions
        var sectionInputIds = command.Sections.Select(s => s.ExamSectionId).ToList();
        if (sectionInputIds.Distinct().Count() != sectionInputIds.Count)
            throw new ValidationException("Duplicate section result submissions are not allowed.");

        // Validate all submitted sections exist in this exam
        foreach (var secInput in command.Sections)
        {
            if (!dbSections.TryGetValue(secInput.ExamSectionId, out var dbSec))
                throw new ValidationException($"ExamSection '{secInput.ExamSectionId}' does not belong to Exam '{examId}'.");

            // Validate question count integrity
            if (secInput.Correct + secInput.Wrong + secInput.Blank != dbSec.QuestionCount)
                throw new ValidationException(
                    $"Section '{dbSec.Id}' total (Correct: {secInput.Correct} + Wrong: {secInput.Wrong} + Blank: {secInput.Blank} = {secInput.Correct + secInput.Wrong + secInput.Blank}) must equal QuestionCount ({dbSec.QuestionCount}).");

            // Validate topics if provided
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

                // Validate topics belong to section subject
                var validTopics = await academicCatalog.ValidateSubjectTopicsAsync(dbSec.SubjectId, topicIds, ct);
                if (!validTopics)
                    throw new ValidationException($"One or more topics do not belong to Subject '{dbSec.SubjectId}'.");
            }
        }

        // Determine attempt number
        int attemptNum;
        if (command.AttemptNumber.HasValue)
        {
            attemptNum = command.AttemptNumber.Value;
        }
        else
        {
            var maxAttempt = await db.Set<StudentExamAttempt>()
                .Where(a => a.ExamId == examId && a.StudentId == student.Id)
                .Select(a => (int?)a.AttemptNumber)
                .MaxAsync(ct);
            attemptNum = (maxAttempt ?? 0) + 1;
        }

        var attemptExists = await db.Set<StudentExamAttempt>()
            .AnyAsync(a => a.ExamId == examId && a.StudentId == student.Id && a.AttemptNumber == attemptNum, ct);
        if (attemptExists)
            throw new ConflictException($"Attempt #{attemptNum} already exists for this student and exam.");

        // Calculate nets and totals
        int totalCorrect = 0, totalWrong = 0, totalBlank = 0;
        decimal totalNet = 0m;

        var subjectResults = new List<StudentExamSubjectResult>();
        var topicResults = new List<StudentExamTopicResult>();

        var attemptId = Guid.NewGuid();

        foreach (var secInput in command.Sections)
        {
            var dbSec = dbSections[secInput.ExamSectionId];
            var penalty = dbSec.WrongAnswerPenalty ?? exam.WrongAnswerPenalty;

            var secNet = scoringService.CalculateNet(secInput.Correct, secInput.Wrong, penalty);

            totalCorrect += secInput.Correct;
            totalWrong += secInput.Wrong;
            totalBlank += secInput.Blank;
            totalNet += secNet;

            subjectResults.Add(new StudentExamSubjectResult
            {
                Id = Guid.NewGuid(),
                InstitutionId = student.InstitutionId,
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
                    topicResults.Add(new StudentExamTopicResult
                    {
                        Id = Guid.NewGuid(),
                        InstitutionId = student.InstitutionId,
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

        var attempt = new StudentExamAttempt
        {
            Id = attemptId,
            InstitutionId = student.InstitutionId,
            ExamId = examId,
            StudentId = student.Id,
            AttemptNumber = attemptNum,
            TakenAt = command.TakenAt,
            Status = AttemptStatus.Draft,
            TotalCorrect = totalCorrect,
            TotalWrong = totalWrong,
            TotalBlank = totalBlank,
            TotalNet = totalNet,
            ReportedScore = command.ReportedScore,
            ScoreSource = command.ScoreSource,
            SubjectResults = subjectResults,
            TopicResults = topicResults
        };

        db.Set<StudentExamAttempt>().Add(attempt);

        if (currentUser.UserId.HasValue)
        {
            await auditService.LogAsync(currentUser.UserId.Value, "ExamAttemptCreated", "StudentExamAttempt", attempt.Id.ToString(), student.InstitutionId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);

        return attempt.Id;
    }
}
