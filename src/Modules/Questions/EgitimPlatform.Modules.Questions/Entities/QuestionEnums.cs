namespace EgitimPlatform.Modules.Questions.Entities;

public enum QuestionType
{
    MultipleChoice = 1
}

public enum DifficultyLevel
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public enum QuestionSourceType
{
    InstitutionCustom = 1,
    OsymPast = 2,
    Publisher = 3
}

public enum QuestionStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}
