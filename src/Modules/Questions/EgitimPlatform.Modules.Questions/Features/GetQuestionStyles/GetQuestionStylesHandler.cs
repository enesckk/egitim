using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Questions.Features.GetQuestionStyles;

public class GetQuestionStylesHandler(IApplicationDbContext db)
{
    public async Task<List<QuestionStyleDto>> HandleAsync(CancellationToken ct = default)
    {
        return await db.Set<QuestionStyle>()
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .Select(s => new QuestionStyleDto(
                s.Id,
                s.Code,
                s.Name,
                s.Description,
                s.DisplayOrder
            ))
            .ToListAsync(ct);
    }
}
