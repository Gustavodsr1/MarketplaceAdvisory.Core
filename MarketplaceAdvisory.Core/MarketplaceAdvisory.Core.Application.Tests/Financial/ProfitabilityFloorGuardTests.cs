using FluentAssertions;
using MarketplaceAdvisory.Core.Application.Financial;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Tests.Financial;

/// <summary>
/// Tests for the Application-layer floor guard. Emphasizes the three branches from
/// research.md#R2: Allowed / HeldFloorHit / RequiresOverride.
/// </summary>
public sealed class ProfitabilityFloorGuardTests
{
    private static ProfitabilityInputs BaseInputs(decimal salePrice) =>
        new(
            MarketplaceType.MercadoLivre,
            Cmv: new Money(50m, "BRL"),
            SalePrice: new Money(salePrice, "BRL"),
            CommissionPercentage: new Percentage(12m),
            FixedUnitFee: new Money(5m, "BRL"),
            SubsidizedShippingCost: new Money(18m, "BRL"),
            TaxRate: new Percentage(6m),
            Regime: TaxRegime.SimplesNacional,
            Thresholds: new ProfitabilityThresholds(new Percentage(12m), new Percentage(6m), new Percentage(5m)));

    private static ProfitabilityFloorGuard NewGuard() => new(new ProfitabilityCalculatorService());

    [Fact]
    public async Task PriceAboveFloor_ReturnsAllowed()
    {
        var guard = NewGuard();
        var decision = await guard.EvaluateAsync(BaseInputs(salePrice: 200m), overrideToken: null, CancellationToken.None);

        decision.Should().BeOfType<FloorDecision.Allowed>();
    }

    [Fact]
    public async Task PriceBelowFloor_ReturnsHeldFloorHit()
    {
        var guard = NewGuard();
        var decision = await guard.EvaluateAsync(BaseInputs(salePrice: 60m), overrideToken: null, CancellationToken.None);

        decision.Should().BeOfType<FloorDecision.HeldFloorHit>();
    }

    [Fact]
    public async Task PriceBelowFloor_WithOverride_RequiresOverride()
    {
        var guard = NewGuard();
        var token = new HumanOverrideToken("user:42", "MARKETING_PROMO", acceptedLossAmount: 10m);

        var decision = await guard.EvaluateAsync(BaseInputs(salePrice: 60m), token, CancellationToken.None);

        decision.Should().BeOfType<FloorDecision.RequiresOverride>();
    }
}
