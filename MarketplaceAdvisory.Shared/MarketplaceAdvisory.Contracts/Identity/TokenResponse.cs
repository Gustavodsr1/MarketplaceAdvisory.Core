namespace MarketplaceAdvisory.Contracts.Identity;

/// <summary>
/// The issued access token returned by the Identity provider and consumed by clients.
/// </summary>
/// <param name="AccessToken">The signed JWT.</param>
/// <param name="TokenType">The token scheme, always "Bearer".</param>
/// <param name="ExpiresInSeconds">Lifetime of the token in seconds.</param>
public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresInSeconds);
