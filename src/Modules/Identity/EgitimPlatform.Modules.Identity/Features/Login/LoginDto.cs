namespace EgitimPlatform.Modules.Identity.Features.Login;

/// <summary>
/// P2-3: Refresh token removed from response DTO.
/// Raw refresh tokens are NEVER exposed to JavaScript.
/// The refresh token is delivered exclusively via HttpOnly Secure cookie.
/// </summary>
public sealed record LoginResponseDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string TokenType = "Bearer");
