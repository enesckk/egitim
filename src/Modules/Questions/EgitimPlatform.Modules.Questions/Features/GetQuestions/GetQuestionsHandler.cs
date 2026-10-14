using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.BuildingBlocks.Pagination;
using EgitimPlatform.Modules.Questions.Entities;
using EgitimPlatform.Modules.Questions.Features.GetQuestionStyles;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Questions.Features.GetQuestions;

public record QuestionDto(
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
    int OptionCount,
    List<QuestionStyleDto> Styles,
    DateTimeOffset CreatedAt
);

public record GetQuestionsQuery(
    Guid? SubjectId = null,
    Guid? TopicId = null,
    Guid? SubTopicId = null,
    Guid? LearningOutcomeId = null,
    DifficultyLevel? Difficulty = null,
    QuestionSourceType? SourceType = null,
    int? SourceYear = null,
    QuestionStatus? Status = null,
    string? SearchText = null,
    string? PoolType = null, // "global", "institution", "all"
    Guid? QuestionStyleId = null,
    int Page = 1,
    int PageSize = 20
);

public class GetQuestionsQueryValidator : AbstractValidator<GetQuestionsQuery>
{
    public GetQuestionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetQuestionsHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser)
{
    public async Task<PaginatedList<QuestionDto>> HandleAsync(GetQuestionsQuery query, CancellationToken ct = default)
    {
        await new GetQuestionsQueryValidator().ValidateAndThrowAsync(query, ct);

        var queryable = db.Set<Question>().AsNoTracking();

        // Security Tenant Filter:
        if (currentUser.IsSuperAdmin)
        {
            if (string.Equals(query.PoolType, "global", StringComparison.OrdinalIgnoreCase))
            {
                queryable = queryable.Where(q => q.InstitutionId == null);
            }
            else if (string.Equals(query.PoolType, "institution", StringComparison.OrdinalIgnoreCase))
            {
                queryable = queryable.Where(q => q.InstitutionId != null);
            }
            // else "all" or null -> no tenant filter for SuperAdmin
        }
        else
        {
            var userInstId = await currentUser.GetInstitutionIdAsync();
            if (!userInstId.HasValue)
            {
                // Unauthenticated or institution-less user can only view Global questions
                queryable = queryable.Where(q => q.InstitutionId == null);
            }
            else
            {
                if (string.Equals(query.PoolType, "global", StringComparison.OrdinalIgnoreCase))
                {
                    queryable = queryable.Where(q => q.InstitutionId == null);
                }
                else if (string.Equals(query.PoolType, "institution", StringComparison.OrdinalIgnoreCase))
                {
                    queryable = queryable.Where(q => q.InstitutionId == userInstId.Value);
                }
                else
                {
                    // Default for tenant user: Global pool OR Own tenant pool
                    queryable = queryable.Where(q => q.InstitutionId == null || q.InstitutionId == userInstId.Value);
                }
            }
        }

        // Filters
        if (query.SubjectId.HasValue)
            queryable = queryable.Where(q => q.SubjectId == query.SubjectId.Value);

        if (query.TopicId.HasValue)
            queryable = queryable.Where(q => q.TopicId == query.TopicId.Value);

        if (query.SubTopicId.HasValue)
            queryable = queryable.Where(q => q.SubTopicId == query.SubTopicId.Value);

        if (query.LearningOutcomeId.HasValue)
            queryable = queryable.Where(q => q.LearningOutcomeId == query.LearningOutcomeId.Value);

        if (query.Difficulty.HasValue)
            queryable = queryable.Where(q => q.Difficulty == query.Difficulty.Value);

        if (query.SourceType.HasValue)
            queryable = queryable.Where(q => q.SourceType == query.SourceType.Value);

        if (query.SourceYear.HasValue)
            queryable = queryable.Where(q => q.SourceYear == query.SourceYear.Value);

        if (query.Status.HasValue)
            queryable = queryable.Where(q => q.Status == query.Status.Value);

        if (query.QuestionStyleId.HasValue)
            queryable = queryable.Where(q => q.StyleAssignments.Any(sa => sa.QuestionStyleId == query.QuestionStyleId.Value));

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim();
            queryable = queryable.Where(q => q.Content.Contains(search));
        }

        // Projection & Deterministic Ordering
        var projectedQuery = queryable
            .OrderByDescending(q => q.CreatedAt)
            .ThenBy(q => q.Id)
            .Select(q => new QuestionDto(
                q.Id,
                q.InstitutionId,
                q.SubjectId,
                q.TopicId,
                q.SubTopicId,
                q.LearningOutcomeId,
                q.QuestionType,
                q.Difficulty,
                q.SourceType,
                q.SourceYear,
                q.Content,
                q.MediaUrl,
                q.Status,
                q.Options.Count,
                q.StyleAssignments
                    .OrderBy(sa => sa.QuestionStyle!.DisplayOrder)
                    .ThenBy(sa => sa.QuestionStyle!.Name)
                    .Select(sa => new QuestionStyleDto(
                        sa.QuestionStyle!.Id,
                        sa.QuestionStyle!.Code,
                        sa.QuestionStyle!.Name,
                        sa.QuestionStyle!.Description,
                        sa.QuestionStyle!.DisplayOrder
                    )).ToList(),
                q.CreatedAt
            ));

        return await PaginatedList<QuestionDto>.CreateAsync(projectedQuery, query.Page, query.PageSize, ct);
    }
}
