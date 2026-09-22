using System.Security.Claims;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.SharedKernel.Authentication;

namespace MarketplaceAdvisory.Core.Api.Authentication;

/// <summary>
/// Resolves the authenticated caller from the validated JWT on the current HTTP request.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(AppClaimTypes.Subject);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(AppClaimTypes.Role).Select(claim => claim.Value).ToArray() ?? [];
}
