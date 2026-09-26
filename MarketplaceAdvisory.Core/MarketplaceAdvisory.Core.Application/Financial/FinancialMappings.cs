using MarketplaceAdvisory.Contracts.Financial;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Financial;

/// <summary>
/// Explicit Domain → Contracts mapping for the Financial module. Kept as hand-written,
/// deterministic projections instead of Mapster because the value objects (Money, Percentage,
/// enum → string) map trivially and clarity matters more than convention here.
/// </summary>
internal static class FinancialMappings
{
    public static MoneyDto ToDto(this Money money) => new(money.Amount, money.Currency);

    public static ProductProfitabilityDto ToProfitabilityDto(
        MarketplaceType marketplace,
        Money salePrice,
        ProfitabilityResult result) =>
        new(
            marketplace.ToString(),
            salePrice.ToDto(),
            result.NetProfit.ToDto(),
            result.NetMarginPercentage.Value,
            result.Status.ToString(),
            result.MinimumPriceFloor.ToDto());
}
