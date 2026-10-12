using EgitimPlatform.BuildingBlocks.Constants;
using EgitimPlatform.BuildingBlocks.Exceptions;
using EgitimPlatform.BuildingBlocks.Interfaces;
using EgitimPlatform.Modules.Exams.Features.AcademicIntelligence;
using EgitimPlatform.Modules.Planning.Entities;
using Microsoft.EntityFrameworkCore;

namespace EgitimPlatform.Modules.Planning.Features;

public class AcceptAcademicStudyPlanHandler(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IStudentDirectory studentDirectory,
    GetStudentAcademicStudyPlanHandler studyPlanHandler)
{
    public async Task<StudyPlanDetailDto> HandleAsync(AcceptAcademicStudyPlanCommand command, CancellationToken ct = default)
    {
        // 1. Authorize & Resolve Student Reference
        var student = await studentDirectory.GetStudentAsync(command.StudentId, ct)
            ?? throw new NotFoundException("Student not found.");

        await ValidateAccessAsync(student, ct);

        // 2. Active Plan Uniqueness Rule: Reject if student already has an Active study plan
        var existingActivePlan = await db.Set<StudyPlan>()
            .AsNoTracking()
            .AnyAsync(p => p.StudentId == command.StudentId && p.Status == StudyPlanStatus.Active && !p.IsDeleted, ct);

        if (existingActivePlan)
        {
            throw new ConflictException("Student already has an active study plan. Complete or cancel the active plan before creating a new one.");
        }

        // 3. Server-Authoritative Proposal Regeneration
        var query = new AcademicStudyPlanQuery(
            StudentId: command.StudentId,
            ExamTypeId: command.ExamTypeId,
            WindowSize: command.WindowSize,
            Limit: command.Limit,
            RecommendationLimit: command.RecommendationLimit,
            PlanDays: command.PlanDays,
            DailyStudyMinutes: command.DailyStudyMinutes,
            StartDate: command.StartDate,
            From: command.From,
            To: command.To
        );

        var proposal = await studyPlanHandler.GetStudyPlanAsync(query, ct);

        // 4. Reject Empty Proposal
        var totalTaskCount = proposal.Days.Sum(d => d.Sessions.Count);
        if (totalTaskCount == 0 || proposal.TotalPlannedMinutes == 0)
        {
            throw new FluentValidation.ValidationException("Cannot accept an empty study plan proposal.");
        }

        var now = DateTimeOffset.UtcNow;
        var startDate = command.StartDate ?? proposal.Days.FirstOrDefault()?.Date ?? now;
        var endDate = startDate.AddDays(proposal.PlanDays);

        // 5. Construct Persistent Entities
        var plan = new StudyPlan
        {
            Id = Guid.NewGuid(),
            InstitutionId = student.InstitutionId,
            StudentId = student.Id,
            Title = $"Akademik Çalışma Planı ({proposal.PlanDays} Gün)",
            StartDate = startDate,
            EndDate = endDate,
            Status = StudyPlanStatus.Active,
            Source = StudyPlanSource.AcademicIntelligence,
            RecommendationPolicyVersion = proposal.RecommendationPolicyVersion,
            StudyPlanPolicyVersion = proposal.StudyPlanPolicyVersion,
            ActivatedAt = now,
            PlanDays = proposal.PlanDays,
            DailyStudyMinutes = proposal.DailyStudyMinutes,
            SessionMinutes = proposal.SessionMinutes,
            TotalAvailableMinutes = proposal.TotalAvailableMinutes,
            TotalPlannedMinutes = proposal.TotalPlannedMinutes,
            UnallocatedMinutes = proposal.UnallocatedMinutes,
            UtilizationRate = proposal.UtilizationRate,
            ScheduledRecommendationCount = proposal.ScheduledRecommendationCount,
            UnscheduledRecommendationCount = proposal.UnscheduledRecommendationCount,
            CreatedAt = now,
        };

        var tasks = new List<StudyTask>();
        foreach (var day in proposal.Days)
        {
            foreach (var session in day.Sessions)
            {
                var task = new StudyTask
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = student.InstitutionId,
                    StudyPlanId = plan.Id,
                    StudentId = student.Id,
                    DayIndex = session.DayIndex,
                    PlannedDate = session.Date,
                    Sequence = session.Sequence,
                    DurationMinutes = session.DurationMinutes,
                    NodeType = session.NodeType,
                    NodeId = session.NodeId,
                    SubjectId = session.SubjectId,
                    SubjectName = session.SubjectName,
                    TopicId = session.TopicId,
                    TopicName = session.TopicName,
                    SubTopicId = session.SubTopicId,
                    SubTopicName = session.SubTopicName,
                    LearningOutcomeId = session.LearningOutcomeId,
                    LearningOutcomeName = session.LearningOutcomeName,
                    TitleSnapshot = session.Name,
                    ClassificationStateSnapshot = session.ClassificationState,
                    PriorityScoreSnapshot = session.PriorityScore,
                    RecommendationType = session.RecommendationType,
                    IsCompleted = false,
                    CompletedAt = null,
                    CreatedAt = now,
                };
                tasks.Add(task);
            }
        }

        // 6. Atomic Save
        try
        {
            db.Set<StudyPlan>().Add(plan);
            db.Set<StudyTask>().AddRange(tasks);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new ConflictException("Student already has an active study plan. Complete or cancel the active plan before creating a new one.", ex);
        }

        // 7. Project to Detail DTO
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
            CompletedTaskCount: 0,
            TotalTaskCount: tasks.Count,
            RemainingTaskCount: tasks.Count,
            CompletedTaskMinutes: 0,
            TaskProgressRate: 0m,
            MinuteProgressRate: 0m,
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
        if (currentUser.IsInRole(Roles.Coach))
        {
            throw new ForbiddenException("Coach cannot modify student study plans.");
        }

        if (currentUser.IsInRole(Roles.Teacher) && !currentUser.IsSuperAdmin && !currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            throw new ForbiddenException("Teacher role cannot modify student study plans.");
        }

        if (currentUser.IsSuperAdmin) return;

        var currentInstId = await currentUser.GetInstitutionIdAsync();
        if (!currentInstId.HasValue || currentInstId.Value != student.InstitutionId)
        {
            throw new ForbiddenException("Access denied across institution boundary.");
        }

        if (currentUser.IsInRole(Roles.Student))
        {
            if (currentUser.UserId != student.UserId)
            {
                throw new ForbiddenException("Student can only create plans for themselves.");
            }
            return;
        }

        if (currentUser.IsInRole(Roles.InstitutionAdmin))
        {
            return;
        }

        if (currentUser.IsInRole(Roles.Parent))
        {
            throw new ForbiddenException("Parent cannot accept student study plans.");
        }

        throw new ForbiddenException("Access denied.");
    }
}
