using MarketplaceAdvisory.Core.Domain.Common;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Subsidized shipping cost for a volumetric-weight tier per marketplace. Tiers are platform-wide
/// (no tenant override in v1) and MUST cover the range of possible weights without gaps
/// (invariant enforced by the seed / import job, not by this aggregate).
/// </summary>
public sealed class ShippingTier : AggregateRoot<Guid>
{
    private ShippingTier(
        Guid id,
        MarketplaceType marketplace,
        int minVolumetricWeightGrams,
        int maxVolumetricWeightGrams,
        Money subsidizedCost,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil)
        : base(id, new TenantId(Guid.Empty))
    {
        Marketplace = marketplace;
        MinVolumetricWeightGrams = minVolumetricWeightGrams;
        MaxVolumetricWeightGrams = maxVolumetricWeightGrams;
        SubsidizedCost = subsidizedCost;
        EffectiveFrom = effectiveFrom;
        EffectiveUntil = effectiveUntil;
    }

    private ShippingTier() : base(Guid.Empty, default)
    {
        SubsidizedCost = Money.Zero("BRL");
    }

    public MarketplaceType Marketplace { get; private set; }

    public int MinVolumetricWeightGrams { get; private set; }

    public int MaxVolumetricWeightGrams { get; private set; }

    public Money SubsidizedCost { get; private set; }

    public DateTimeOffset EffectiveFrom { get; private set; }

    public DateTimeOffset? EffectiveUntil { get; private set; }

    public static ShippingTier Create(
        MarketplaceType marketplace,
        int minVolumetricWeightGrams,
        int maxVolumetricWeightGrams,
        Money subsidizedCost,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveUntil = null)
    {
        if (minVolumetricWeightGrams < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minVolumetricWeightGrams));
        }

        if (maxVolumetricWeightGrams <= minVolumetricWeightGrams)
        {
            throw new ArgumentException("Max weight must be greater than min weight.", nameof(maxVolumetricWeightGrams));
        }

        return new ShippingTier(
            Guid.NewGuid(),
            marketplace,
            minVolumetricWeightGrams,
            maxVolumetricWeightGrams,
            subsidizedCost,
            effectiveFrom,
            effectiveUntil);
    }

    public bool Covers(int volumetricWeightGrams) =>
        volumetricWeightGrams >= MinVolumetricWeightGrams
        && volumetricWeightGrams < MaxVolumetricWeightGrams;

    public bool IsActiveOn(DateTimeOffset at) =>
        at >= EffectiveFrom && (EffectiveUntil is null || at < EffectiveUntil.Value);
}
