namespace EgitimPlatform.Modules.Questions.Entities;

/// <summary>
/// Many-to-many junction entity associating a Question with a QuestionStyle.
/// Composite primary key: (QuestionId, QuestionStyleId).
/// </summary>
public class QuestionStyleAssignment
{
    public Guid QuestionId { get; set; }
    public Question? Question { get; set; }

    public Guid QuestionStyleId { get; set; }
    public QuestionStyle? QuestionStyle { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
