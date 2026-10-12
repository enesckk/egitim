using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Entities;
using EgitimPlatform.Modules.Exams.Services;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Exams.Features.GetAttemptQuestionResults;

public record StudentQuestionResultDto(
    Guid ExamQuestionId,
    Guid QuestionId,
    int QuestionNumber,
    string Content,
    string? SelectedOption,
    string? CorrectOption,
    bool? IsCorrect,
    bool IsBlank,
    decimal NetContribution
);

public class GetAttemptQuestionResultsHandler(
    IApplicationDbContext db,
    IExamResourceAccess resourceAccess,
    ICurrentUser currentUser)
{
    public async Task<List<StudentQuestionResultDto>> HandleAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await resourceAccess.ValidateAttemptReadAccessAsync(attemptId, ct);

        var isStudentOrParent = currentUser.IsInRole(Roles.Student) || currentUser.IsInRole(Roles.Parent);
        if (isStudentOrParent && attempt.Status != AttemptStatus.Finalized)
        {
            throw new ForbiddenException("Question results and answer key cannot be accessed until the attempt is finalized.");
        }

        var examQuestions = await db.Set<ExamQuestion>()
            .AsNoTracking()
            .Where(eq => eq.ExamId == attempt.ExamId)
            .OrderBy(eq => eq.Order)
            .ThenBy(eq => eq.QuestionNumber)
            .ToListAsync(ct);

        var questionIds = examQuestions.Select(eq => eq.QuestionId).Distinct().ToList();

        var questions = await db.Set<Question>()
            .AsNoTracking()
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, ct);

        var questionResults = await db.Set<StudentExamQuestionResult>()
            .AsNoTracking()
            .Where(r => r.StudentExamAttemptId == attemptId)
            .ToDictionaryAsync(r => r.ExamQuestionId, ct);

        var list = new List<StudentQuestionResultDto>();

        foreach (var eq in examQuestions)
        {
            var q = questions.GetValueOrDefault(eq.QuestionId);
            var res = questionResults.GetValueOrDefault(eq.Id);

            list.Add(new StudentQuestionResultDto(
                eq.Id,
                eq.QuestionId,
                eq.QuestionNumber,
                q?.Content ?? string.Empty,
                res?.SelectedOption,
                eq.CorrectOption,
                res?.IsCorrect,
                res?.IsBlank ?? true,
                res?.NetContribution ?? 0m
            ));
        }

        return list;
    }
}
