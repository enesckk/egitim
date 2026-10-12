namespace EgitimPlatform.Api.Seeding;

/// <summary>
/// Controls the Demo/Staging showcase dataset seeding.
/// Disabled by default. Must be explicitly enabled via configuration.
/// NEVER automatically runs in Production unless explicitly and securely enabled.
/// </summary>
public sealed class DemoSeedSettings
{
    public const string SectionName = "DemoSeed";

    /// <summary>
    /// Whether Demo dataset seeding is enabled. Default: false.
    /// </summary>
    public bool Enabled { get; set; } = false;

    public string AdminEmail { get; set; } = "admin.demo@egitim.com";
    public string? AdminPassword { get; set; }

    public string TeacherEmail { get; set; } = "teacher.demo@egitim.com";
    public string? TeacherPassword { get; set; }

    public string CoachEmail { get; set; } = "coach.demo@egitim.com";
    public string? CoachPassword { get; set; }

    public string StudentEmail { get; set; } = "student.demo@egitim.com";
    public string? StudentPassword { get; set; }

    public string ParentEmail { get; set; } = "parent.demo@egitim.com";
    public string? ParentPassword { get; set; }
}
