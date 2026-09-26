using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Immutable snapshot of every input that fed a profitability calculation. Persisted alongside
/// <see cref="ProfitabilityCalculation"/> so an audit can reproduce the decision byte-for-byte
/// (Success Criteria SC-008).
/// </summary>
public sealed record ProfitabilityInputs(
    MarketplaceType Marketplace,
    Money Cmv,
    Money SalePrice,
    Percentage CommissionPercentage,
    Money FixedUnitFee,
    Money SubsidizedShippingCost,
    Percentage TaxRate,
    TaxRegime Regime,
    ProfitabilityThresholds Thresholds);
