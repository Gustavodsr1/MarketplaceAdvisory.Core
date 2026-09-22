namespace MarketplaceAdvisory.SharedKernel.Authentication;

/// <summary>
/// Application roles used for Role-Based Access Control (RBAC).
/// Shared by the Identity provider (token issuing) and the APIs (token validation).
/// </summary>
public static class AppRoles
{
    public const string User = "User";
    public const string Manager = "Manager";
}
