using System.Security.Cryptography;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Questions.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ExamResultImportEntity = EgitimPlatform.Modules.Exams.Entities.ExamResultImport;

namespace EgitimPlatform.Modules.Exams.Features.ExamResultImport;

public class CommitResultImportHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IExamResourceAccess resourceAccess,
    IExamResultImportPreviewEngine previewEngine,
    IExamScoringService scoringService,
    IRawExamAttemptFinalizer rawAttemptFinalizer,
    IAuditService auditService,
    IAggregateExamAttemptCreator? aggregateAttemptCreator = null,
    int batchSize = 200)
{
    public async Task<ExamResultImportCommitResultDto> HandleAsync(
        Guid examId,
        ImportType importType,
        string fileHash,
        IFormFile file,
        CancellationToken ct = default)
    {
        var exam = await resourceAccess.ValidateExamAccessAsync(examId, ct);

        if (string.IsNullOrWhiteSpace(fileHash))
        {
            throw new ValidationException("Preview fileHash is required for commit confirmation.");
        }

        // 1. Validate file header & size limits, then calculate streaming SHA-256 hash and compare
        new ImportFileValidator().ValidateFileHeader(file);

        string actualHash;
        await using (var stream = file.OpenReadStream())
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            var hashBytes = await SHA256.HashDataAsync(stream, ct);
            actualHash = Convert.ToHexString(hashBytes);
        }

        if (!string.Equals(actualHash, fileHash.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("IMPORT_FILE_CHANGED: The uploaded file does not match the file previously previewed.");
        }

        // 2. Idempotency Check: Prevent duplicate import of the exact same workbook
        var existingImport = await db.Set<ExamResultImportEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.InstitutionId == exam.InstitutionId &&
                e.ExamId == examId &&
                e.ImportType == importType &&
                e.FileHash == actualHash &&
                e.Status == ImportStatus.Completed, ct);

        if (existingImport != null)
        {
            throw new ConflictException("IMPORT_ALREADY_COMPLETED: This result file has already been imported for this exam.");
        }

        // 3. Re-run Full Sprint 5A Parsing & Validation Pipeline
        var preview = await previewEngine.GeneratePreviewAsync(examId, exam.InstitutionId, importType, file, ct);

        if (!preview.CanImport)
        {
            throw new ValidationException("IMPORT_VALIDATION_FAILED: The import workbook contains validation errors and cannot be committed.");
        }

        if (preview.Rows.Count == 0)
        {
            throw new ValidationException("Import workbook contains no valid student result rows.");
        }

        // 4. Atomic All-or-Nothing Transaction
        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var createdAttempts = new List<StudentExamAttempt>();

            // Pre-fetch current max attempt numbers per student for this exam
            var studentIds = preview.Rows.Select(r => r.MatchedStudent!.Id).Distinct().ToList();
            var maxAttemptNumbers = await db.Set<StudentExamAttempt>()
                .Where(a => a.ExamId == examId && studentIds.Contains(a.StudentId))
                .GroupBy(a => a.StudentId)
                .Select(g => new { StudentId = g.Key, MaxAttempt = g.Max(a => a.AttemptNumber) })
                .ToDictionaryAsync(x => x.StudentId, x => x.MaxAttempt, ct);

            if (importType == ImportType.AggregateResults)
            {
                var sectionsDict = await db.Set<ExamSection>()
                    .AsNoTracking()
                    .Where(s => s.ExamId == examId)
                    .ToDictionaryAsync(s => s.Id, ct);

                foreach (var row in preview.Rows)
                {
                    var studentId = row.MatchedStudent!.Id;
                    var currentMax = maxAttemptNumbers.GetValueOrDefault(studentId, 0);
                    int attemptNum = currentMax + 1;
                    maxAttemptNumbers[studentId] = attemptNum;

                    var sectionCounts = row.Sections.Select(s => (s.ExamSectionId, s.Correct, s.Wrong, s.Blank));

                    var effectiveCreator = aggregateAttemptCreator ?? new AggregateExamAttemptCreator(scoringService);
                    var attempt = effectiveCreator.CreateFinalizedAggregateAttempt(
                        exam.InstitutionId,
                        examId,
                        studentId,
                        attemptNum,
                        sectionsDict,
                        exam,
                        sectionCounts,
                        now,
                        takenAt: exam.ExamDate);

                    createdAttempts.Add(attempt);
                }
            }
            else if (importType == ImportType.QuestionAnswers)
            {
                // Pre-load exam sections & questions
                var sectionsDict = await db.Set<ExamSection>()
                    .AsNoTracking()
                    .Where(s => s.ExamId == examId)
                    .ToDictionaryAsync(s => s.Id, ct);

                var examQuestions = await db.Set<ExamQuestion>()
                    .AsNoTracking()
                    .Where(eq => eq.ExamId == examId)
                    .OrderBy(eq => eq.QuestionNumber)
                    .ToListAsync(ct);

                var questionIds = examQuestions.Select(eq => eq.QuestionId).Distinct().ToList();
                var questionTopics = await db.Set<Question>()
                    .AsNoTracking()
                    .Where(q => questionIds.Contains(q.Id))
                    .ToDictionaryAsync(q => q.Id, q => (Guid?)q.TopicId, ct);

                foreach (var row in preview.Rows)
                {
                    var studentId = row.MatchedStudent!.Id;
                    var currentMax = maxAttemptNumbers.GetValueOrDefault(studentId, 0);
                    int attemptNum = currentMax + 1;
                    maxAttemptNumbers[studentId] = attemptNum;

                    var attemptId = Guid.NewGuid();
                    var answerMap = row.Answers.ToDictionary(a => a.ExamQuestionId);

                    var rawAnswers = new List<StudentExamQuestionResult>();
                    foreach (var eq in examQuestions)
                    {
                        string? selectedOpt = null;
                        bool isBlank = true;

                        if (answerMap.TryGetValue(eq.Id, out var parsedAns))
                        {
                            if (!parsedAns.IsBlank && !string.IsNullOrWhiteSpace(parsedAns.SelectedOption))
                            {
                                selectedOpt = parsedAns.SelectedOption.Trim().ToUpperInvariant();
                                isBlank = false;
                            }
                        }

                        rawAnswers.Add(new StudentExamQuestionResult
                        {
                            Id = Guid.NewGuid(),
                            InstitutionId = exam.InstitutionId,
                            StudentExamAttemptId = attemptId,
                            ExamQuestionId = eq.Id,
                            QuestionId = eq.QuestionId,
                            SelectedOption = selectedOpt,
                            IsBlank = isBlank,
                            IsCorrect = null,
                            NetContribution = 0m
                        });
                    }

                    var attempt = new StudentExamAttempt
                    {
                        Id = attemptId,
                        InstitutionId = exam.InstitutionId,
                        ExamId = examId,
                        StudentId = studentId,
                        AttemptNumber = attemptNum,
                        TakenAt = exam.ExamDate,
                        Status = AttemptStatus.Finalized,
                        FinalizedAt = now,
                        IsRawAvailable = true
                    };

                    rawAttemptFinalizer.FinalizeRawAttempt(
                        attempt,
                        exam,
                        sectionsDict,
                        examQuestions,
                        rawAnswers,
                        questionTopics,
                        scoringService,
                        now);

                    createdAttempts.Add(attempt);
                }
            }

            // Perform batched EF persistence to keep memory footprint low for large imports
            int effectiveBatchSize = batchSize > 0 ? batchSize : 200;
            for (int i = 0; i < createdAttempts.Count; i += effectiveBatchSize)
            {
                var batch = createdAttempts.Skip(i).Take(effectiveBatchSize);
                db.Set<StudentExamAttempt>().AddRange(batch);
                await db.SaveChangesAsync(ct);
            }

            var importLedger = new ExamResultImportEntity
            {
                Id = Guid.NewGuid(),
                InstitutionId = exam.InstitutionId,
                ExamId = examId,
                ImportType = importType,
                FileHash = actualHash,
                TemplateVersion = preview.TemplateVersion,
                TotalRows = preview.TotalRows,
                ImportedStudentsCount = preview.ValidRowsCount,
                CreatedAttemptsCount = createdAttempts.Count,
                ImportedByUserId = currentUser.UserId ?? Guid.Empty,
                ImportedAt = now,
                Status = ImportStatus.Completed
            };

            db.Set<ExamResultImportEntity>().Add(importLedger);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            if (currentUser.UserId.HasValue)
            {
                await auditService.LogAsync(currentUser.UserId.Value, "ExamResultImportCompleted", "ExamResultImport", importLedger.Id.ToString(), exam.InstitutionId, cancellationToken: ct);
            }

            return new ExamResultImportCommitResultDto(
                importLedger.Id,
                examId,
                actualHash,
                preview.ValidRowsCount,
                createdAttempts.Count,
                now);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("IMPORT_ALREADY_COMPLETED: A duplicate import request for this workbook was already processed.");
        }
    }
}
