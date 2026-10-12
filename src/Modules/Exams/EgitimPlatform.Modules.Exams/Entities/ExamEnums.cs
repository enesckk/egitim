namespace EgitimPlatform.Modules.Exams.Entities;

public enum ExamStatus
{
    Draft = 0,
    Published = 1,
    Completed = 2,
    Cancelled = 3,
}

public enum AttemptStatus
{
    InProgress = 0,
    Completed = 1,
    Abandoned = 2,
}

public enum ScoreSource
{
    Automatic = 0,
    Manual = 1,
    Imported = 2,
}
