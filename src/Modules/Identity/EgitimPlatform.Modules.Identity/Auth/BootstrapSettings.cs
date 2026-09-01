namespace EgitimPlatform.Modules.Identity.Auth;

/// <summary>
/// Controls the initial SuperAdmin bootstrap. Disabled by default.
/// Must be explicitly enabled via configuration with credentials from a
/// secure source (environment variables, secret manager, user-secrets).
/// NEVER commit real credentials to source.
/// </summary>
public sealed class BootstrapSettings
{
    public const string SectionName = "Bootstrap";

    /// <summary>
    /// Whether SuperAdmin bootstrap is enabled. Default: false.
    /// Should only be true in development/test environments.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Email for the initial SuperAdmin account.
    /// Required when Enabled = true.
    /// </summary>
    public string? SuperAdminEmail { get; set; }

    /// <summary>
    /// Password for the initial SuperAdmin account.
    /// Required when Enabled = true.
    /// Must meet Identity password policy.
    /// </summary>
    public string? SuperAdminPassword { get; set; }
}
