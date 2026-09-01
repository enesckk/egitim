namespace EgitimPlatform.Modules.Identity.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    private static readonly HashSet<string> KnownPlaceholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "CHANGE_ME_IN_PRODUCTION_MIN_32_CHARS_LONG_KEY_VALUE!!",
        "CHANGE_ME",
        "CHANGE_ME!",
        "YOUR-SECRET-KEY",
        "YOUR_SECRET_KEY",
        "PLACEHOLDER",
        "TODO",
        "REPLACE_ME",
        "super-secret-key",
        "test-key",
        "dev-key",
    };

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Validates JWT settings at startup. Throws if the signing key is missing,
    /// too short, or a known placeholder.
    /// NEVER logs the signing key value.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException(
                "JwtSettings:SigningKey is not configured. " +
                "Provide a secure signing key via environment variables or secret manager. " +
                "Minimum length: 32 characters.");
        }

        if (SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JwtSettings:SigningKey must be at least 32 characters. " +
                "Current length is too short for secure operation.");
        }

        if (KnownPlaceholders.Contains(SigningKey))
        {
            throw new InvalidOperationException(
                "JwtSettings:SigningKey contains a known placeholder value. " +
                "Replace with a real secret from environment variables or secret manager.");
        }

        // Detect common placeholder patterns (case-insensitive)
        var upper = SigningKey.ToUpperInvariant();
        if (upper.Contains("CHANGE_ME") ||
            upper.Contains("PLACEHOLDER") ||
            upper.Contains("REPLACE_ME") ||
            upper.Contains("INSERT_KEY") ||
            upper.Contains("TODO_"))
        {
            throw new InvalidOperationException(
                "JwtSettings:SigningKey appears to contain a placeholder value. " +
                "Replace with a real secret from environment variables or secret manager.");
        }
    }
}
