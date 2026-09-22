using MarketplaceAdvisory.SharedKernel.Tenancy;

namespace MarketplaceAdvisory.Core.Domain.Common;

/// <summary>
/// Base aggregate root. Every aggregate is owned by a tenant, enforcing multi-tenancy
/// at the domain level.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    protected AggregateRoot(TId id, TenantId tenantId) : base(id) => TenantId = tenantId;

    public TenantId TenantId { get; }
}
