using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.CreateManualResult;

public class CreateSingleManualResultHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IExamResourceAccess resourceAccess,
    IStudentDirectory studentDirectory,
    IAggregateExamAttemptCreator aggregateAttemptCreator,
    IAuditService auditService)
{
    public async Task<SingleManualResultResponseDto> HandleAsync(
        Guid examId,
        CreateSingleManualResultCommand command,
        CancellationToken ct = default)
    {
        var exam = await resourceAccess.ValidateExamAccessAsync(examId, ct);

        if (command.StudentId == Guid.Empty)
        {
            throw new ValidationException("STUDENT_NOT_FOUND: Valid StudentId is required.");
        }

        var student = await studentDirectory.GetStudentAsync(command.StudentId, ct);

        if (student == null || student.InstitutionId != exam.InstitutionId)
        {
            throw new NotFoundException("STUDENT_NOT_FOUND: Student does not exist or does not belong to the institution.");
        }

        if (command.Sections == null || command.Sections.Count == 0)
        {
            throw new ValidationException("EMPTY_RESULT_REQUEST: At least one section result must be provided.");
        }

        // Input parameter validation (counts and section IDs)
        var submittedSectionIds = new HashSet<Guid>();
        foreach (var secInput in command.Sections)
        {
            if (secInput.ExamSectionId == Guid.Empty)
            {
                throw new ValidationException("SECTION_NOT_FOUND: ExamSectionId cannot be empty.");
            }

            if (!submittedSectionIds.Add(secInput.ExamSectionId))
            {
                throw new ValidationException($"DUPLICATE_SECTION_RESULT: Duplicate section ID '{secInput.ExamSectionId}' in request.");
            }

            if (secInput.Correct < 0)
            {
                throw new ValidationException("INVALID_CORRECT_COUNT: Correct count cannot be negative.");
            }

            if (secInput.Wrong < 0)
            {
                throw new ValidationException("INVALID_WRONG_COUNT: Wrong count cannot be negative.");
            }

            if (secInput.Blank < 0)
            {
                throw new ValidationException("INVALID_BLANK_COUNT: Blank count cannot be negative.");
            }
        }

        // Load exam sections
        var examSections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == examId)
            .ToListAsync(ct);

        var sectionsDict = examSections.ToDictionary(s => s.Id);

        // Validate section mapping and question count invariants
        foreach (var secInput in command.Sections)
        {
            if (!sectionsDict.TryGetValue(secInput.ExamSectionId, out var sec))
            {
                throw new ValidationException($"SECTION_NOT_IN_EXAM: Section '{secInput.ExamSectionId}' does not belong to Exam '{examId}'.");
            }

            if (secInput.Correct + secInput.Wrong + secInput.Blank != sec.QuestionCount)
            {
                throw new ValidationException($"QUESTION_COUNT_MISMATCH: Total questions ({secInput.Correct + secInput.Wrong + secInput.Blank}) for section '{sec.Id}' does not equal section count ({sec.QuestionCount}).");
            }
        }

        // Validate exact section coverage
        foreach (var sec in examSections)
        {
            if (!submittedSectionIds.Contains(sec.Id))
            {
                throw new ValidationException($"MISSING_SECTION_RESULT: Missing result input for section '{sec.Id}'. All exam sections must be provided.");
            }
        }

        // Calculate Next AttemptNumber
        var currentMax = await db.Set<StudentExamAttempt>()
            .Where(a => a.ExamId == examId && a.StudentId == command.StudentId)
            .MaxAsync(a => (int?)a.AttemptNumber, ct) ?? 0;

        int nextAttemptNum = currentMax + 1;
        var now = DateTimeOffset.UtcNow;

        var sectionCounts = command.Sections.Select(s => (s.ExamSectionId, s.Correct, s.Wrong, s.Blank));

        var attempt = aggregateAttemptCreator.CreateFinalizedAggregateAttempt(
            exam.InstitutionId,
            examId,
            command.StudentId,
            nextAttemptNum,
            sectionsDict,
            exam,
            sectionCounts,
            now,
            takenAt: exam.ExamDate);

        // Atomic Transaction
        try
        {
            await using var transaction = await db.BeginTransactionAsync(ct);
            db.Set<StudentExamAttempt>().Add(attempt);
            await db.SaveChangesAsync(ct);

            if (currentUser.UserId.HasValue)
            {
                await auditService.LogAsync(currentUser.UserId.Value, "ManualExamResultCreated", "StudentExamAttempt", attempt.Id.ToString(), exam.InstitutionId, cancellationToken: ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new ConflictException("ATTEMPT_CONCURRENCY_CONFLICT: A result attempt with the same attempt number already exists for this student and exam.", ex);
        }

        return new SingleManualResultResponseDto(
            attempt.Id,
            attempt.StudentId,
            attempt.ExamId,
            attempt.AttemptNumber,
            attempt.TotalCorrect,
            attempt.TotalWrong,
            attempt.TotalBlank,
            attempt.TotalNet,
            attempt.IsRawAvailable,
            attempt.FinalizedAt!.Value);
    }
}
