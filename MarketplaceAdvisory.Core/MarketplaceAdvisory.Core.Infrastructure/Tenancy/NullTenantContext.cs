using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.SharedKernel.Tenancy;

namespace MarketplaceAdvisory.Core.Infrastructure.Tenancy;

/// <summary>
/// Default tenant context used by tooling (migrations) and any host that does not resolve a
/// tenant from an HTTP request. The API layer replaces this with an HTTP-aware implementation.
/// </summary>
public sealed class NullTenantContext : ITenantContext
{
    public TenantId? TenantId => null;
}
