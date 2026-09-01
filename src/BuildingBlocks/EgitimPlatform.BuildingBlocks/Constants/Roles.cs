namespace EgitimPlatform.BuildingBlocks.Constants;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string InstitutionAdmin = "InstitutionAdmin";
    public const string Coach = "Coach";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string Parent = "Parent";

    public static IReadOnlyList<string> All { get; } =
    [
        SuperAdmin,
        InstitutionAdmin,
        Coach,
        Teacher,
        Student,
        Parent
    ];
}
