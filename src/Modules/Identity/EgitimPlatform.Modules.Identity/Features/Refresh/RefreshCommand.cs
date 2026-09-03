namespace EgitimPlatform.Modules.Identity.Features.Refresh;

public sealed record RefreshCommand(string RefreshToken);

/// <summary>
/// Internal result from RefreshHandler — includes the new raw token for cookie setting.
/// The raw token is NOT serialized to the JSON response body.
/// </summary>
public sealed record RefreshResult(
    string AccessToken,
    string NewRawRefreshToken,
    DateTimeOffset AccessTokenExpiresAt);
