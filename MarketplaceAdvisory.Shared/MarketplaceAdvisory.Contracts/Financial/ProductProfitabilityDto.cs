namespace MarketplaceAdvisory.Contracts.Financial;

/// <summary>
/// Profitability projection for a SKU on a single marketplace — the payload behind the
/// traffic-light dashboard and the product profitability view.
/// </summary>
public sealed record ProductProfitabilityDto(
    string Marketplace,
    MoneyDto SalePrice,
    MoneyDto NetProfit,
    decimal NetMarginPercent,
    string Status,
    MoneyDto MinimumPriceFloor);
