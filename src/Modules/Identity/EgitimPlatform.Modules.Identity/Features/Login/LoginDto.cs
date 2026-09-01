namespace EgitimPlatform.Modules.Identity.Features.Login;

public sealed record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string TokenType = "Bearer");
