namespace EgitimPlatform.Modules.Questions.Features.GetQuestionStyles;

public record QuestionStyleDto(
    Guid Id,
    string Code,
    string Name,
    string? Description = null,
    int DisplayOrder = 0
);
