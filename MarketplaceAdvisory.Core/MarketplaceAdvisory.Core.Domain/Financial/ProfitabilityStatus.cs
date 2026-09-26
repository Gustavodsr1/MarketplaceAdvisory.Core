namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Traffic-light classification of a product's profitability. Emitted by
/// <see cref="ProfitabilityCalculatorService"/> per Rule A2 in the Financial Engine spec.
/// </summary>
public enum ProfitabilityStatus
{
    /// <summary>Margin at or above the tenant's Green threshold.</summary>
    Green = 1,

    /// <summary>Margin between the Yellow threshold and the Green threshold.</summary>
    Yellow = 2,

    /// <summary>Margin below the Yellow threshold (includes any negative margin).</summary>
    Red = 3
}
