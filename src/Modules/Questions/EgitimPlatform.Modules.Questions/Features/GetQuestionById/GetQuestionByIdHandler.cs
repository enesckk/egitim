using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Questions.Features.GetQuestionById;

public record QuestionOptionDetailDto(
    Guid Id,
    string OptionLabel,
    string Content,
    bool IsCorrect
);

public record QuestionDetailDto(
    Guid Id,
    Guid? InstitutionId,
    Guid SubjectId,
    Guid TopicId,
    Guid? SubTopicId,
    Guid? LearningOutcomeId,
    QuestionType QuestionType,
    DifficultyLevel Difficulty,
    QuestionSourceType SourceType,
    int? SourceYear,
    string Content,
    string? MediaUrl,
    QuestionStatus Status,
    List<QuestionOptionDetailDto> Options,
    DateTimeOffset CreatedAt
);

public class GetQuestionByIdHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
{
    public async Task<QuestionDetailDto> HandleAsync(Guid questionId, CancellationToken ct = default)
    {
        var question = await db.Set<Question>()
            .Include(q => q.Options)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            throw new NotFoundException($"Question with ID '{questionId}' was not found.");

        // Tenant Security Access Check:
        // Allowed if: SuperAdmin OR question is Global (InstitutionId == null) OR question belongs to current user's institution.
        if (!currentUser.IsSuperAdmin && question.InstitutionId.HasValue)
        {
            var userInstId = await currentUser.GetInstitutionIdAsync();
            if (!userInstId.HasValue || userInstId.Value != question.InstitutionId.Value)
            {
                // Security-safe 404 to avoid leaking cross-tenant question existence
                throw new NotFoundException($"Question with ID '{questionId}' was not found.");
            }
        }

        return new QuestionDetailDto(
            question.Id,
            question.InstitutionId,
            question.SubjectId,
            question.TopicId,
            question.SubTopicId,
            question.LearningOutcomeId,
            question.QuestionType,
            question.Difficulty,
            question.SourceType,
            question.SourceYear,
            question.Content,
            question.MediaUrl,
            question.Status,
            question.Options.Select(o => new QuestionOptionDetailDto(
                o.Id,
                o.OptionLabel,
                o.Content,
                o.IsCorrect
            )).ToList(),
            question.CreatedAt
        );
    }
}
