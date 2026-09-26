using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.SharedKernel.Primitives;

namespace MarketplaceAdvisory.Core.Domain.Tenants;

/// <summary>
/// Tenant tax-regime selection with an effective window. Regimes don't overlap for a tenant.
/// </summary>
public sealed class TaxRegimeSelection : ValueObject
{
    public TaxRegimeSelection(TaxRegime regime, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveUntil = null)
    {
        if (effectiveUntil.HasValue && effectiveUntil.Value <= effectiveFrom)
        {
            throw new ArgumentException("EffectiveUntil must be after EffectiveFrom.");
        }

        Regime = regime;
        EffectiveFrom = effectiveFrom;
        EffectiveUntil = effectiveUntil;
    }

    public TaxRegime Regime { get; }

    public DateTimeOffset EffectiveFrom { get; }

    public DateTimeOffset? EffectiveUntil { get; }

    public bool IsActiveOn(DateTimeOffset at) =>
        at >= EffectiveFrom && (EffectiveUntil is null || at < EffectiveUntil.Value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Regime;
        yield return EffectiveFrom;
        yield return EffectiveUntil;
    }
}
