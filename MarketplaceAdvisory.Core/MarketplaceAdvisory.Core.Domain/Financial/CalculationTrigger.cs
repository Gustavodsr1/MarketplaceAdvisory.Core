namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Reason a <see cref="ProfitabilityCalculation"/> was produced. Persisted for audit so we
/// can explain post-hoc why a status changed (spec §Success Criteria SC-008).
/// </summary>
public enum CalculationTrigger
{
    SalePriceChange = 1,
    CmvChange = 2,
    FeeChange = 3,
    ShippingChange = 4,
    TaxChange = 5,
    ThresholdChange = 6,
    SimulatorRun = 7
}
