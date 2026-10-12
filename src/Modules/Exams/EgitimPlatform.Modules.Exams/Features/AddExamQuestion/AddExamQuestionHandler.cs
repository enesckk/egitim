using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.AddExamQuestion;

public record AddExamQuestionCommand(
    Guid ExamSectionId,
    Guid QuestionId,
    int QuestionNumber,
    int Order
);

public class AddExamQuestionCommandValidator : AbstractValidator<AddExamQuestionCommand>
{
    public AddExamQuestionCommandValidator()
    {
        RuleFor(x => x.ExamSectionId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.QuestionNumber).GreaterThan(0);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}

public class AddExamQuestionHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAuditService auditService)
{
    public async Task<Guid> HandleAsync(Guid examId, AddExamQuestionCommand command, CancellationToken ct = default)
    {
        await new AddExamQuestionCommandValidator().ValidateAndThrowAsync(command, ct);

        var userInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentUser.IsSuperAdmin && !userInstId.HasValue)
            throw new ForbiddenException("Institution context required to manage exam questions.");

        var institutionId = currentUser.IsSuperAdmin && userInstId.HasValue ? userInstId.Value : (userInstId ?? Guid.Empty);

        // Fetch Exam & Section
        var exam = await db.Set<Exam>()
            .Include(e => e.Sections)
            .FirstOrDefaultAsync(e => e.Id == examId && (currentUser.IsSuperAdmin || e.InstitutionId == institutionId), ct);

        if (exam is null)
            throw new NotFoundException($"Exam with ID '{examId}' was not found.");

        var section = exam.Sections.FirstOrDefault(s => s.Id == command.ExamSectionId);
        if (section is null)
            throw new ValidationException($"ExamSection with ID '{command.ExamSectionId}' does not belong to Exam '{examId}'.");

        var effectiveInstitutionId = exam.InstitutionId;

        // Fetch Question
        var question = await db.Set<Question>()
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == command.QuestionId, ct);

        if (question is null)
            throw new NotFoundException($"Question with ID '{command.QuestionId}' was not found.");

        // CROSS-TENANT SECURITY CHECK: Question MUST be Global (InstitutionId == null) OR match Exam's InstitutionId
        if (question.InstitutionId.HasValue && question.InstitutionId.Value != effectiveInstitutionId)
        {
            // Security-safe 404 response to prevent cross-tenant information leaks
            throw new NotFoundException($"Question with ID '{command.QuestionId}' was not found.");
        }

        // Status Validation: Only Published questions can be added to an exam
        if (question.Status != QuestionStatus.Published)
        {
            throw new ValidationException($"Question '{command.QuestionId}' is in '{question.Status}' status. Only Published questions can be added to an exam.");
        }

        // Check duplicate QuestionNumber in Section
        var numberExists = await db.Set<ExamQuestion>()
            .AnyAsync(eq => eq.ExamSectionId == command.ExamSectionId && eq.QuestionNumber == command.QuestionNumber && !eq.IsDeleted, ct);

        if (numberExists)
            throw new ConflictException($"Question number '{command.QuestionNumber}' is already used in this exam section.");

        // Check duplicate Question in Section
        var questionAlreadyAdded = await db.Set<ExamQuestion>()
            .AnyAsync(eq => eq.ExamSectionId == command.ExamSectionId && eq.QuestionId == command.QuestionId && !eq.IsDeleted, ct);

        if (questionAlreadyAdded)
            throw new ConflictException($"Question '{command.QuestionId}' is already added to this exam section.");

        // SERVER SNAPSHOT CORRECT OPTION
        var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
        if (correctOption is null)
            throw new ValidationException($"Question '{command.QuestionId}' does not have a correct option configured.");

        var examQuestion = new ExamQuestion
        {
            Id = Guid.NewGuid(),
            InstitutionId = effectiveInstitutionId,
            ExamId = examId,
            ExamSectionId = command.ExamSectionId,
            QuestionId = command.QuestionId,
            QuestionNumber = command.QuestionNumber,
            Order = command.Order,
            CorrectOption = correctOption.OptionLabel.Trim().ToUpperInvariant() // Server-side snapshot
        };

        db.Set<ExamQuestion>().Add(examQuestion);

        if (currentUser.UserId.HasValue)
        {
            await auditService.LogAsync(currentUser.UserId.Value, "ExamQuestionAdded", "ExamQuestion", examQuestion.Id.ToString(), effectiveInstitutionId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);

        return examQuestion.Id;
    }
}
