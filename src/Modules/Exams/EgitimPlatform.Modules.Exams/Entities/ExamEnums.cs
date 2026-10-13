namespace EgitimPlatform.Modules.Exams.Entities;

public enum ExamStatus
{
    Draft = 0,
    Published = 1,
    Closed = 2
}

public enum AttemptStatus
{
    Draft = 0,
    Finalized = 1
}

public enum ScoreSource
{
    None = 0,
    Institution = 1,
    Provider = 2
}
