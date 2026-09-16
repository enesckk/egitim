using System.Diagnostics;
using System.Text.Json;
using EgitimPlatform.Modules.Students.Entities;
namespace EgitimPlatform.Modules.Students.Services;
public static class GoalHistory
{
    public static string Snapshot(StudentGoal g) => JsonSerializer.Serialize(new
    {
        g.Id, g.StudentId, g.InstitutionId, g.Title, g.Description, g.TargetExamTypeId,
        g.TargetScore, g.TargetRank, g.TargetSchoolName, g.EffectiveDate, g.IsActive, g.IsDeleted
    });
    public static StudentGoalHistory Capture(StudentGoal goal, string action, Guid? actor, string? before = null) => new()
    {
        StudentGoalId = goal.Id, Action = action, PreviousValuesJson = before, NewValuesJson = Snapshot(goal),
        ChangedAt = DateTimeOffset.UtcNow, ChangedBy = actor,
        CorrelationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N")
    };
}
