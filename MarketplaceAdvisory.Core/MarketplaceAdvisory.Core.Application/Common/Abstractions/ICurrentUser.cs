namespace MarketplaceAdvisory.Core.Application.Common.Abstractions;

/// <summary>
/// Ambient information about the authenticated caller, resolved from the JWT in the API layer.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }
}
