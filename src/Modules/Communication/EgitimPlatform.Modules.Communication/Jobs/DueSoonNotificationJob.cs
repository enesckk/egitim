using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;

using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Communication.Jobs;

public class DueSoonNotificationJob(
    IApplicationDbContext db,
    INotificationService notificationService)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var in24Hours = now.AddHours(24);

        // 1. Coach Tasks due within next 24 hours
        var dueCoachTasks = await db.Set<CoachTask>()
            .AsNoTracking()
            .Where(t => t.Status == CoachTaskStatus.Active
                     && t.DueDate.HasValue
                     && t.DueDate.Value >= now
                     && t.DueDate.Value <= in24Hours
                     && !t.IsDeleted)
            .ToListAsync(ct);

        foreach (var task in dueCoachTasks)
        {
            // Find student's UserId
            var studentUserId = await db.Set<EgitimPlatform.Modules.Students.Entities.Student>()
                .Where(s => s.Id == task.StudentId && !s.IsDeleted)
                .Select(s => s.UserId)
                .FirstOrDefaultAsync(ct);

            if (studentUserId.HasValue)
            {
                await notificationService.CreateNotificationAsync(
                    institutionId: task.InstitutionId,
                    userId: studentUserId.Value,
                    type: NotificationType.CoachTaskDueSoon,
                    title: "Koç Görevi Yaklaşıyor",
                    message: $"'{task.Title}' başlıklı koç görevinin teslim tarihi yaklaşıyor.",
                    relatedEntityType: "CoachTask",
                    relatedEntityId: task.Id,
                    ct: ct);
            }
        }

        // 2. Study Tasks due soon
        var dueStudyTasks = await db.Set<StudyTask>()
            .AsNoTracking()
            .Where(t => !t.IsCompleted
                     && t.PlannedDate.HasValue
                     && t.PlannedDate.Value >= now
                     && t.PlannedDate.Value <= in24Hours
                     && !t.IsDeleted)
            .Take(100)
            .ToListAsync(ct);

        foreach (var task in dueStudyTasks)
        {
            var studentUserId = await db.Set<EgitimPlatform.Modules.Students.Entities.Student>()
                .Where(s => s.Id == task.StudentId && !s.IsDeleted)
                .Select(s => s.UserId)
                .FirstOrDefaultAsync(ct);

            if (studentUserId.HasValue)
            {
                await notificationService.CreateNotificationAsync(
                    institutionId: task.InstitutionId,
                    userId: studentUserId.Value,
                    type: NotificationType.StudyPlanTaskDueSoon,
                    title: "Çalışma Görevi Yaklaşıyor",
                    message: $"'{task.TitleSnapshot}' çalışma planı görevinin süresi yaklaşıyor.",
                    relatedEntityType: "StudyTask",
                    relatedEntityId: task.Id,
                    ct: ct);
            }
        }
    }
}
