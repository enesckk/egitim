using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.CreateManualResult;

public class CreateBulkManualResultHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IExamResourceAccess resourceAccess,
    IStudentDirectory studentDirectory,
    IAggregateExamAttemptCreator aggregateAttemptCreator,
    IAuditService auditService)
{
    public const int MaxBulkLimit = 500;

    public async Task<BulkManualResultResponseDto> HandleAsync(
        Guid examId,
        CreateBulkManualResultCommand command,
        CancellationToken ct = default)
    {
        var exam = await resourceAccess.ValidateExamAccessAsync(examId, ct);

        if (command.StudentResults == null || command.StudentResults.Count == 0)
        {
            throw new ValidationException("EMPTY_RESULT_REQUEST: Bulk manual result request contains no student entries.");
        }

        if (command.StudentResults.Count > MaxBulkLimit)
        {
            throw new ValidationException($"BULK_LIMIT_EXCEEDED: Bulk manual result limit of {MaxBulkLimit} students per request exceeded.");
        }

        // Validate no duplicate StudentIds in request
        var seenStudentIds = new HashSet<Guid>();
        foreach (var row in command.StudentResults)
        {
            if (row.StudentId == Guid.Empty)
            {
                throw new ValidationException("STUDENT_NOT_FOUND: Valid StudentId is required for all entries.");
            }

            if (!seenStudentIds.Add(row.StudentId))
            {
                throw new ValidationException($"DUPLICATE_STUDENT_RESULT: Duplicate student ID '{row.StudentId}' in bulk request.");
            }
        }

        // Batch pre-load active students belonging to institution via IStudentDirectory abstraction
        var activeStudentsList = await studentDirectory.GetActiveStudentsForImportAsync(exam.InstitutionId, ct);
        var activeStudentsDict = activeStudentsList.ToDictionary(s => s.Id);

        foreach (var sId in seenStudentIds)
        {
            if (!activeStudentsDict.ContainsKey(sId))
            {
                throw new ValidationException($"STUDENT_NOT_FOUND: Student '{sId}' does not exist or does not belong to the institution.");
            }
        }

        // Pre-load exam sections
        var examSections = await db.Set<ExamSection>()
            .AsNoTracking()
            .Where(s => s.ExamId == examId)
            .ToListAsync(ct);

        var sectionsDict = examSections.ToDictionary(s => s.Id);

        // Pre-load current max attempt numbers per student
        var studentIds = seenStudentIds.ToList();
        var maxAttemptNumbers = await db.Set<StudentExamAttempt>()
            .Where(a => a.ExamId == examId && studentIds.Contains(a.StudentId))
            .GroupBy(a => a.StudentId)
            .Select(g => new { StudentId = g.Key, MaxAttempt = g.Max(a => a.AttemptNumber) })
            .ToDictionaryAsync(x => x.StudentId, x => x.MaxAttempt, ct);

        var now = DateTimeOffset.UtcNow;
        var createdAttempts = new List<StudentExamAttempt>();

        // Validate all rows before performing any database writes
        foreach (var row in command.StudentResults)
        {
            if (row.Sections == null || row.Sections.Count == 0)
            {
                throw new ValidationException($"EMPTY_RESULT_REQUEST: Student '{row.StudentId}' has no section result entries.");
            }

            var submittedSectionIds = new HashSet<Guid>();
            foreach (var secInput in row.Sections)
            {
                if (secInput.ExamSectionId == Guid.Empty)
                {
                    throw new ValidationException($"SECTION_NOT_FOUND: ExamSectionId cannot be empty for student '{row.StudentId}'.");
                }

                if (!submittedSectionIds.Add(secInput.ExamSectionId))
                {
                    throw new ValidationException($"DUPLICATE_SECTION_RESULT: Duplicate section ID '{secInput.ExamSectionId}' for student '{row.StudentId}'.");
                }

                if (!sectionsDict.TryGetValue(secInput.ExamSectionId, out var sec))
                {
                    throw new ValidationException($"SECTION_NOT_IN_EXAM: Section '{secInput.ExamSectionId}' does not belong to Exam '{examId}'.");
                }

                if (secInput.Correct < 0)
                {
                    throw new ValidationException($"INVALID_CORRECT_COUNT: Correct count cannot be negative for section '{sec.Id}' (student '{row.StudentId}').");
                }

                if (secInput.Wrong < 0)
                {
                    throw new ValidationException($"INVALID_WRONG_COUNT: Wrong count cannot be negative for section '{sec.Id}' (student '{row.StudentId}').");
                }

                if (secInput.Blank < 0)
                {
                    throw new ValidationException($"INVALID_BLANK_COUNT: Blank count cannot be negative for section '{sec.Id}' (student '{row.StudentId}').");
                }

                if (secInput.Correct + secInput.Wrong + secInput.Blank != sec.QuestionCount)
                {
                    throw new ValidationException($"QUESTION_COUNT_MISMATCH: Total questions ({secInput.Correct + secInput.Wrong + secInput.Blank}) for section '{sec.Id}' does not equal section count ({sec.QuestionCount}) for student '{row.StudentId}'.");
                }
            }

            foreach (var sec in examSections)
            {
                if (!submittedSectionIds.Contains(sec.Id))
                {
                    throw new ValidationException($"MISSING_SECTION_RESULT: Missing result input for section '{sec.Id}' for student '{row.StudentId}'.");
                }
            }

            int currentMax = maxAttemptNumbers.GetValueOrDefault(row.StudentId, 0);
            int nextAttemptNum = currentMax + 1;
            maxAttemptNumbers[row.StudentId] = nextAttemptNum;

            var sectionCounts = row.Sections.Select(s => (s.ExamSectionId, s.Correct, s.Wrong, s.Blank));

            var attempt = aggregateAttemptCreator.CreateFinalizedAggregateAttempt(
                exam.InstitutionId,
                examId,
                row.StudentId,
                nextAttemptNum,
                sectionsDict,
                exam,
                sectionCounts,
                now,
                takenAt: exam.ExamDate);

            createdAttempts.Add(attempt);
        }

        // Atomic Transaction for entire bulk request
        try
        {
            await using var transaction = await db.BeginTransactionAsync(ct);
            db.Set<StudentExamAttempt>().AddRange(createdAttempts);
            await db.SaveChangesAsync(ct);

            if (currentUser.UserId.HasValue)
            {
                await auditService.LogAsync(currentUser.UserId.Value, "BulkManualExamResultCreated", "Exam", examId.ToString(), exam.InstitutionId, cancellationToken: ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new ConflictException("ATTEMPT_CONCURRENCY_CONFLICT: A result attempt with the same attempt number already exists for one of the students in this bulk request.", ex);
        }

        int totalSubjectResults = createdAttempts.Sum(a => a.SubjectResults.Count);

        return new BulkManualResultResponseDto(
            examId,
            command.StudentResults.Count,
            createdAttempts.Count,
            totalSubjectResults,
            createdAttempts.Select(a => a.Id).ToList());
    }
}
