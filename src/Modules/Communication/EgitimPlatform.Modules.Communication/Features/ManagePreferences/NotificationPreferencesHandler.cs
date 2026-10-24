using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Communication.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Communication.Features.ManagePreferences;

public interface INotificationPreferencesHandler
{
    Task<NotificationPreferencesDto> GetPreferencesAsync(CancellationToken ct = default);
    Task<NotificationPreferencesDto> UpdatePreferencesAsync(UpdateNotificationPreferencesRequest req, CancellationToken ct = default);
}

public class NotificationPreferencesHandler : INotificationPreferencesHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public NotificationPreferencesHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private async Task<(Guid institutionId, Guid userId)> ValidateUserContextAsync()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            throw new ForbiddenException("Kimlik doğrulaması gereklidir.");

        var instId = await _currentUser.GetInstitutionIdAsync();
        if (!instId.HasValue && !_currentUser.IsSuperAdmin)
            throw new ForbiddenException("Kurum bağlamı bulunamadı.");

        return (instId ?? Guid.Empty, _currentUser.UserId.Value);
    }

    public async Task<NotificationPreferencesDto> GetPreferencesAsync(CancellationToken ct = default)
    {
        var (instId, userId) = await ValidateUserContextAsync();

        var pref = await _db.Set<UserNotificationPreference>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.InstitutionId == instId && p.UserId == userId && !p.IsDeleted, ct);

        if (pref == null)
        {
            return new NotificationPreferencesDto(
                InAppEnabled: true,
                ExamResultsEnabled: true,
                CoachTasksEnabled: true,
                MeetingsEnabled: true,
                MessagesEnabled: true,
                MaterialsEnabled: true
            );
        }

        return new NotificationPreferencesDto(
            pref.InAppEnabled,
            pref.ExamResultsEnabled,
            pref.CoachTasksEnabled,
            pref.MeetingsEnabled,
            pref.MessagesEnabled,
            pref.MaterialsEnabled
        );
    }

    public async Task<NotificationPreferencesDto> UpdatePreferencesAsync(UpdateNotificationPreferencesRequest req, CancellationToken ct = default)
    {
        var (instId, userId) = await ValidateUserContextAsync();

        var pref = await _db.Set<UserNotificationPreference>()
            .FirstOrDefaultAsync(p => p.InstitutionId == instId && p.UserId == userId && !p.IsDeleted, ct);

        if (pref == null)
        {
            pref = new UserNotificationPreference
            {
                Id = Guid.NewGuid(),
                InstitutionId = instId,
                UserId = userId,
                InAppEnabled = req.InAppEnabled,
                ExamResultsEnabled = req.ExamResultsEnabled,
                CoachTasksEnabled = req.CoachTasksEnabled,
                MeetingsEnabled = req.MeetingsEnabled,
                MessagesEnabled = req.MessagesEnabled,
                MaterialsEnabled = req.MaterialsEnabled,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };
            _db.Set<UserNotificationPreference>().Add(pref);
        }
        else
        {
            pref.InAppEnabled = req.InAppEnabled;
            pref.ExamResultsEnabled = req.ExamResultsEnabled;
            pref.CoachTasksEnabled = req.CoachTasksEnabled;
            pref.MeetingsEnabled = req.MeetingsEnabled;
            pref.MessagesEnabled = req.MessagesEnabled;
            pref.MaterialsEnabled = req.MaterialsEnabled;
            pref.UpdatedAt = DateTimeOffset.UtcNow;
            pref.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync(ct);

        return new NotificationPreferencesDto(
            pref.InAppEnabled,
            pref.ExamResultsEnabled,
            pref.CoachTasksEnabled,
            pref.MeetingsEnabled,
            pref.MessagesEnabled,
            pref.MaterialsEnabled
        );
    }
}
