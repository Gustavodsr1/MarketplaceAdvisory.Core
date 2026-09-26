using MarketplaceAdvisory.Core.Domain.Common;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Marketplace commission + fixed-fee rate table. Tenant-scoped rows override platform defaults
/// (identified by <see cref="TenantId"/> == <c>null</c>).
/// </summary>
public sealed class MarketplaceFee : AggregateRoot<Guid>
{
    private MarketplaceFee(
        Guid id,
        TenantId? tenantId,
        MarketplaceType marketplace,
        string categoryRef,
        Percentage commissionPercentage,
        Money fixedUnitFee,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil)
        : base(id, tenantId ?? new TenantId(Guid.Empty))
    {
        TenantScoped = tenantId.HasValue;
        Marketplace = marketplace;
        CategoryRef = categoryRef;
        CommissionPercentage = commissionPercentage;
        FixedUnitFee = fixedUnitFee;
        EffectiveFrom = effectiveFrom;
        EffectiveUntil = effectiveUntil;
    }

    private MarketplaceFee() : base(Guid.Empty, default)
    {
        CategoryRef = string.Empty;
        CommissionPercentage = Percentage.Zero;
        FixedUnitFee = Money.Zero("BRL");
    }

    public bool TenantScoped { get; private set; }

    public MarketplaceType Marketplace { get; private set; }

    public string CategoryRef { get; private set; }

    public Percentage CommissionPercentage { get; private set; }

    public Money FixedUnitFee { get; private set; }

    public DateTimeOffset EffectiveFrom { get; private set; }

    public DateTimeOffset? EffectiveUntil { get; private set; }

    public static MarketplaceFee CreatePlatformDefault(
        MarketplaceType marketplace,
        string categoryRef,
        Percentage commissionPercentage,
        Money fixedUnitFee,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil = null)
    {
        if (string.IsNullOrWhiteSpace(categoryRef))
        {
            throw new ArgumentException("Category reference is required.", nameof(categoryRef));
        }

        return new MarketplaceFee(
            Guid.NewGuid(),
            tenantId: null,
            marketplace,
            categoryRef,
            commissionPercentage,
            fixedUnitFee,
            effectiveFrom,
            effectiveUntil);
    }

    public static MarketplaceFee CreateTenantOverride(
        TenantId tenantId,
        MarketplaceType marketplace,
        string categoryRef,
        Percentage commissionPercentage,
        Money fixedUnitFee,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil = null)
    {
        if (string.IsNullOrWhiteSpace(categoryRef))
        {
            throw new ArgumentException("Category reference is required.", nameof(categoryRef));
        }

        return new MarketplaceFee(
            Guid.NewGuid(),
            tenantId,
            marketplace,
            categoryRef,
            commissionPercentage,
            fixedUnitFee,
            effectiveFrom,
            effectiveUntil);
    }

    public bool IsActiveOn(DateTimeOffset at) =>
        at >= EffectiveFrom && (EffectiveUntil is null || at < EffectiveUntil.Value);
}
