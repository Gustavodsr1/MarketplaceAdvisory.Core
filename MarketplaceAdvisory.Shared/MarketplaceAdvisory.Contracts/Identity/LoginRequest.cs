namespace MarketplaceAdvisory.Contracts.Identity;

/// <summary>
/// Login payload accepted by the Identity provider's scaffolding endpoints.
/// </summary>
/// <param name="Username">The user identifier (email or username).</param>
/// <param name="TenantId">Optional tenant the user is signing in to. A demo tenant is used when omitted.</param>
public sealed record LoginRequest(string Username, string? TenantId);
