using MarketplaceAdvisory.SharedKernel.Tenancy;

namespace MarketplaceAdvisory.Core.Application.Common.Abstractions;

/// <summary>
/// Resolves the current tenant (from the JWT "tenant_id" claim) for the running request.
/// </summary>
public interface ITenantContext
{
    TenantId? TenantId { get; }
}
