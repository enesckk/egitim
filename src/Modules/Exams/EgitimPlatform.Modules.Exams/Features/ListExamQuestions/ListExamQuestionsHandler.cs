using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.ListExamQuestions;

// Teacher/Admin DTO — Contains CorrectOption and IsCorrect
public record TeacherOptionDto(
    Guid Id,
    string OptionLabel,
    string Content,
    bool IsCorrect
);

public record ExamQuestionTeacherDto(
    Guid Id,
    Guid ExamSectionId,
    Guid QuestionId,
    int QuestionNumber,
    int Order,
    string Content,
    string? MediaUrl,
    string CorrectOption,
    List<TeacherOptionDto> Options
);

// Student DTO — STRICTLY MASKED: NO CorrectOption, NO IsCorrect property
public record StudentOptionDto(
    Guid Id,
    string OptionLabel,
    string Content
);

public record ExamQuestionStudentDto(
    Guid Id,
    Guid ExamSectionId,
    Guid QuestionId,
    int QuestionNumber,
    int Order,
    string Content,
    string? MediaUrl,
    List<StudentOptionDto> Options
);

public class ListExamQuestionsHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
{
    public async Task<object> HandleAsync(Guid examId, bool forceStudentView = false, CancellationToken ct = default)
    {
        var userInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentUser.IsSuperAdmin && !userInstId.HasValue)
            throw new ForbiddenException("Institution context required.");

        var institutionId = currentUser.IsSuperAdmin && userInstId.HasValue ? userInstId.Value : (userInstId ?? Guid.Empty);

        var exam = await db.Set<Exam>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == examId && (currentUser.IsSuperAdmin || e.InstitutionId == institutionId), ct);

        if (exam is null)
            throw new NotFoundException($"Exam with ID '{examId}' was not found.");

        var examQuestions = await db.Set<ExamQuestion>()
            .AsNoTracking()
            .Where(eq => eq.ExamId == examId)
            .OrderBy(eq => eq.Order)
            .ThenBy(eq => eq.QuestionNumber)
            .ToListAsync(ct);

        var questionIds = examQuestions.Select(eq => eq.QuestionId).Distinct().ToList();

        var questions = await db.Set<Question>()
            .Include(q => q.Options)
            .AsNoTracking()
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, ct);

        var isStudentRole = currentUser.IsInRole(Roles.Student) || currentUser.IsInRole(Roles.Parent) || forceStudentView;

        if (isStudentRole)
        {
            // Student DTO Surface — STRICTLY MASKED (Zero CorrectOption / IsCorrect property leakage)
            var studentResult = examQuestions.Select(eq =>
            {
                var q = questions.GetValueOrDefault(eq.QuestionId);
                return new ExamQuestionStudentDto(
                    eq.Id,
                    eq.ExamSectionId,
                    eq.QuestionId,
                    eq.QuestionNumber,
                    eq.Order,
                    q?.Content ?? string.Empty,
                    q?.MediaUrl,
                    q?.Options.OrderBy(o => o.OptionLabel).Select(o => new StudentOptionDto(
                        o.Id,
                        o.OptionLabel,
                        o.Content
                    )).ToList() ?? []
                );
            }).ToList();

            return studentResult;
        }
        else
        {
            // Teacher/Admin DTO Surface — Includes CorrectOption & IsCorrect
            var teacherResult = examQuestions.Select(eq =>
            {
                var q = questions.GetValueOrDefault(eq.QuestionId);
                return new ExamQuestionTeacherDto(
                    eq.Id,
                    eq.ExamSectionId,
                    eq.QuestionId,
                    eq.QuestionNumber,
                    eq.Order,
                    q?.Content ?? string.Empty,
                    q?.MediaUrl,
                    eq.CorrectOption,
                    q?.Options.OrderBy(o => o.OptionLabel).Select(o => new TeacherOptionDto(
                        o.Id,
                        o.OptionLabel,
                        o.Content,
                        o.IsCorrect
                    )).ToList() ?? []
                );
            }).ToList();

            return teacherResult;
        }
    }
}
