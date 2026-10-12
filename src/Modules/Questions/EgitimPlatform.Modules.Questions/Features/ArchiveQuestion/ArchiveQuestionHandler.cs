using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Questions.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Questions.Features.ArchiveQuestion;

public class ArchiveQuestionHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAuditService auditService)
{
    public async Task HandleAsync(Guid questionId, CancellationToken ct = default)
    {
        var question = await db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            throw new NotFoundException($"Question with ID '{questionId}' was not found.");

        if (question.InstitutionId == null)
        {
            if (!currentUser.IsSuperAdmin)
                throw new ForbiddenException("Only SuperAdmin can archive global pool questions.");
        }
        else
        {
            var userInstId = await currentUser.GetInstitutionIdAsync();
            if (!userInstId.HasValue || userInstId.Value != question.InstitutionId.Value)
            {
                throw new NotFoundException($"Question with ID '{questionId}' was not found.");
            }
        }

        question.Status = QuestionStatus.Archived;

        if (currentUser.UserId.HasValue)
        {
            await auditService.LogAsync(currentUser.UserId.Value, "QuestionArchived", "Question", question.Id.ToString(), question.InstitutionId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);
    }
}
