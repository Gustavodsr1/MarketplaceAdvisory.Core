namespace MarketplaceAdvisory.SharedKernel.Authentication;

/// <summary>
/// Custom and standard JWT claim types shared across the Identity provider and the APIs.
/// Short names are used to keep tokens compact and mapping explicit.
/// </summary>
public static class AppClaimTypes
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string TenantId = "tenant_id";
}
