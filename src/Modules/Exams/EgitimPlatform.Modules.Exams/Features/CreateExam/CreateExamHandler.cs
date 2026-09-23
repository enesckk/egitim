using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.CreateExam;

public record CreateExamSectionDto(
    Guid SubjectId,
    int DisplayOrder,
    int QuestionCount,
    decimal? WrongAnswerPenalty
);

public record CreateExamCommand(
    string Code,
    string Title,
    Guid ExamTypeId,
    DateTimeOffset ExamDate,
    decimal WrongAnswerPenalty,
    ScoreSource ScoreSource,
    List<CreateExamSectionDto> Sections,
    Guid? ExplicitInstitutionId = null
);

public class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
{
    public CreateExamCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ExamTypeId).NotEmpty();
        RuleFor(x => x.WrongAnswerPenalty).GreaterThan(0m);
        RuleFor(x => x.Sections).NotEmpty().WithMessage("Exam must have at least one section.");

        RuleForEach(x => x.Sections).ChildRules(section =>
        {
            section.RuleFor(s => s.SubjectId).NotEmpty();
            section.RuleFor(s => s.DisplayOrder).GreaterThanOrEqualTo(0);
            section.RuleFor(s => s.QuestionCount).GreaterThan(0);
            section.When(s => s.WrongAnswerPenalty.HasValue, () =>
            {
                section.RuleFor(s => s.WrongAnswerPenalty!.Value).GreaterThan(0m);
            });
        });
    }
}

public class CreateExamHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAcademicCatalog academicCatalog,
    IAuditService auditService)
{
    public async Task<Guid> HandleAsync(CreateExamCommand command, CancellationToken ct = default)
    {
        await new CreateExamCommandValidator().ValidateAndThrowAsync(command, ct);

        Guid institutionId;
        if (currentUser.IsSuperAdmin && command.ExplicitInstitutionId.HasValue)
        {
            institutionId = command.ExplicitInstitutionId.Value;
        }
        else
        {
            var userInstId = await currentUser.GetInstitutionIdAsync();
            if (!userInstId.HasValue)
                throw new ForbiddenException("Institution context required.");
            institutionId = userInstId.Value;
        }

        // Validate ExamType in taxonomy
        var examType = await academicCatalog.GetExamTypeAsync(command.ExamTypeId, ct);
        if (examType is null)
            throw new ValidationException($"ExamType with ID '{command.ExamTypeId}' does not exist.");

        // Check unique code within institution
        var codeExists = await db.Set<Exam>().AnyAsync(e => e.InstitutionId == institutionId && e.Code == command.Code, ct);
        if (codeExists)
            throw new ConflictException($"An exam with code '{command.Code}' already exists in this institution.");

        // Validate unique subjects in sections
        var subjectIds = command.Sections.Select(s => s.SubjectId).ToList();
        if (subjectIds.Distinct().Count() != subjectIds.Count)
            throw new ValidationException("Duplicate subject sections are not allowed in an exam.");

        // Batch validate subjects belong to ExamType
        foreach (var subjectId in subjectIds.Distinct())
        {
            var isValidSubject = await academicCatalog.ValidateExamTypeSubjectAsync(command.ExamTypeId, subjectId, ct);
            if (!isValidSubject)
                throw new ValidationException($"Subject '{subjectId}' does not belong to ExamType '{command.ExamTypeId}'.");
        }

        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            InstitutionId = institutionId,
            ExamTypeId = command.ExamTypeId,
            Code = command.Code.Trim(),
            Title = command.Title.Trim(),
            ExamDate = command.ExamDate,
            Status = ExamStatus.Draft,
            WrongAnswerPenalty = command.WrongAnswerPenalty,
            ScoreSource = command.ScoreSource,
            Sections = command.Sections.Select(s => new ExamSection
            {
                Id = Guid.NewGuid(),
                InstitutionId = institutionId,
                SubjectId = s.SubjectId,
                DisplayOrder = s.DisplayOrder,
                QuestionCount = s.QuestionCount,
                WrongAnswerPenalty = s.WrongAnswerPenalty
            }).ToList()
        };

        db.Set<Exam>().Add(exam);

        if (currentUser.UserId.HasValue)
        {
            await auditService.LogAsync(currentUser.UserId.Value, "ExamCreated", "Exam", exam.Id.ToString(), institutionId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);

        return exam.Id;
    }
}
