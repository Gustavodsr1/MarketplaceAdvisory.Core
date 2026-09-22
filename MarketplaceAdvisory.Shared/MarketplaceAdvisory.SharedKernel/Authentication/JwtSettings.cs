namespace MarketplaceAdvisory.SharedKernel.Authentication;

/// <summary>
/// JWT configuration shared between the Identity provider (which signs tokens) and the
/// APIs (which validate them). Bind from the "Jwt" configuration section.
/// In production replace the symmetric <see cref="SigningKey"/> with asymmetric keys (RSA/JWKS).
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenMinutes { get; init; } = 60;
}
