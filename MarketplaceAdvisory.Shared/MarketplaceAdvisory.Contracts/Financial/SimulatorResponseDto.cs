namespace MarketplaceAdvisory.Contracts.Financial;

/// <summary>
/// Response of the Ideal-Price Simulator (Rule A3): the sale price that yields the requested
/// target net margin, plus the recomputed margin and the current minimum floor.
/// </summary>
public sealed record SimulatorResponseDto(
    MoneyDto SuggestedSalePrice,
    decimal ComputedMarginPercent,
    string? ShippingTierAtSuggestedPrice,
    MoneyDto MinimumPriceFloor);
