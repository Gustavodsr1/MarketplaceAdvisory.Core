using System.Security.Claims;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.SharedKernel.Authentication;
using MarketplaceAdvisory.SharedKernel.Tenancy;

namespace MarketplaceAdvisory.Core.Api.Authentication;

/// <summary>
/// Resolves the current tenant from the JWT "tenant_id" claim. The tenant is re-validated here
/// and never trusted from a raw header.
/// </summary>
public sealed class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public TenantId? TenantId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(AppClaimTypes.TenantId);
            return MarketplaceAdvisory.SharedKernel.Tenancy.TenantId.TryParse(value, out var tenantId)
                ? tenantId
                : null;
        }
    }
}
