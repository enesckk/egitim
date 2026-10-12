using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features;

public class GetStudyPlanHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory,
    ICoachStudentQuery coachStudentQuery)
{
    public async Task<StudyPlanDetailDto> HandleAsync(Guid studentId, Guid planId, CancellationToken ct = default)
    {
        var student = await studentDirectory.GetStudentAsync(studentId, ct)
            ?? throw new NotFoundException("Student not found.");

        await ValidateAccessAsync(student, ct);

        var plan = await db.Set<StudyPlan>()
            .AsNoTracking()
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == planId && p.StudentId == studentId && p.InstitutionId == student.InstitutionId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Study plan not found.");

        var tasks = plan.Tasks
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.DayIndex)
            .ThenBy(t => t.Sequence)
            .ToList();

        int completedCount = tasks.Count(t => t.IsCompleted);
        int totalCount = tasks.Count;
        int remainingCount = totalCount - completedCount;
        int completedMins = tasks.Where(t => t.IsCompleted).Sum(t => t.DurationMinutes);
        decimal taskProgress = totalCount > 0 ? Math.Round((decimal)completedCount / totalCount, 4) : 0m;
        decimal minProgress = plan.TotalPlannedMinutes > 0 ? Math.Round((decimal)completedMins / plan.TotalPlannedMinutes, 4) : 0m;

        var planDto = new StudyPlanDto(
            plan.Id,
            plan.InstitutionId,
            plan.StudentId,
            plan.Title,
            plan.StartDate,
            plan.EndDate,
            plan.Status,
            plan.Source,
            plan.RecommendationPolicyVersion,
            plan.StudyPlanPolicyVersion,
            plan.ActivatedAt,
            plan.CompletedAt,
            plan.PlanDays,
            plan.DailyStudyMinutes,
            plan.SessionMinutes,
            plan.TotalAvailableMinutes,
            plan.TotalPlannedMinutes,
            plan.UnallocatedMinutes,
            plan.UtilizationRate,
            plan.ScheduledRecommendationCount,
            plan.UnscheduledRecommendationCount,
            CompletedTaskCount: completedCount,
            TotalTaskCount: totalCount,
            RemainingTaskCount: remainingCount,
            CompletedTaskMinutes: completedMins,
            TaskProgressRate: taskProgress,
            MinuteProgressRate: minProgress,
            CreatedAt: plan.CreatedAt
        );

        var taskDtos = tasks.Select(t => new StudyTaskDto(
            t.Id,
            t.StudyPlanId,
            t.StudentId,
            t.DayIndex,
            t.PlannedDate,
            t.Sequence,
            t.DurationMinutes,
            t.NodeType,
            t.NodeId,
            t.SubjectId,
            t.SubjectName,
            t.TopicId,
            t.TopicName,
            t.SubTopicId,
            t.SubTopicName,
            t.LearningOutcomeId,
            t.LearningOutcomeName,
            t.TitleSnapshot,
            t.ClassificationStateSnapshot,
            t.PriorityScoreSnapshot,
            t.RecommendationType,
            t.IsCompleted,
            t.CompletedAt
        )).ToList();

        return new StudyPlanDetailDto(planDto, taskDtos);
    }

    private async Task ValidateAccessAsync(StudentReferenceDto student, CancellationToken ct)
    {
        if (currentUser.IsSuperAdmin) return;

        var currentInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentInstId.HasValue || currentInstId.Value != student.InstitutionId)
        {
            throw new ForbiddenException("Access denied across institution boundary.");
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Teacher role cannot access student study plans.");
        }

        if (currentUser.IsInRole(Roles.Student) && currentUser.UserId != student.UserId)
        {
            throw new ForbiddenException("Student can only view their own study plan.");
        }

        if (currentUser.IsInRole(Roles.Coach))
        {
            if (!currentUser.UserId.HasValue)
            {
                throw new ForbiddenException("Authenticated coach context is missing user identity.");
            }

            var isAssigned = await coachStudentQuery.HasActiveAssignmentAsync(
                currentUser.UserId.Value,
                currentInstId.Value,
                student.Id,
                ct);

            if (!isAssigned)
            {
                throw new ForbiddenException("Coach is not actively assigned to this student.");
            }
        }

        if (currentUser.IsInRole(Roles.Parent))
        {
            if (!currentUser.UserId.HasValue)
            {
                throw new ForbiddenException("Authenticated parent context is missing user identity.");
            }

            var isRelated = await studentDirectory.IsParentRelatedToStudentAsync(
                currentUser.UserId.Value,
                student.Id,
                currentInstId.Value,
                ct);

            if (!isRelated)
            {
                throw new ForbiddenException("Parent is not linked to this student.");
            }
        }
    }
}
