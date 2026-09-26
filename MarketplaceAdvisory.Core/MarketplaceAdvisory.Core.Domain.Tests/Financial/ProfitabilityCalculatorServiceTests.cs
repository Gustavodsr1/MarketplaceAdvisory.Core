using FluentAssertions;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Tests.Financial;

/// <summary>
/// Golden tests for the Financial Engine. Numbers come straight from the spec acceptance
/// scenarios so any drift in the math fails a very obvious test.
/// </summary>
public sealed class ProfitabilityCalculatorServiceTests
{
    private static ProfitabilityThresholds StandardThresholds =>
        new(new Percentage(12m), new Percentage(6m), new Percentage(0m));

    private static ProfitabilityInputs SpecScenario_Yellow(decimal salePrice = 100m) =>
        new(
            MarketplaceType.MercadoLivre,
            Cmv: new Money(50m, "BRL"),
            SalePrice: new Money(salePrice, "BRL"),
            CommissionPercentage: new Percentage(12m),
            FixedUnitFee: new Money(5m, "BRL"),
            SubsidizedShippingCost: new Money(18m, "BRL"),
            TaxRate: new Percentage(6m),
            Regime: TaxRegime.SimplesNacional,
            Thresholds: StandardThresholds);

    [Fact]
    public void SpecScenario1_Yields_Yellow_And_Correct_Net_Profit()
    {
        // From spec.md User Story 1 Scenario 1:
        //   CMV=50, commission=12%, fee=5, shipping=18, tax=6%, price=100 → profit=9, margin=9%.
        var calc = new ProfitabilityCalculatorService();
        var result = calc.Calculate(SpecScenario_Yellow());

        result.NetProfit.Amount.Should().Be(9m);
        result.NetMarginPercentage.Value.Should().Be(9m);
        result.Status.Should().Be(ProfitabilityStatus.Yellow);
    }

    [Fact]
    public void SpecScenario2_LowerPrice_Yields_Red_And_NegativeProfit()
    {
        // Same inputs but sale price = 70 → margin < 0.
        var calc = new ProfitabilityCalculatorService();
        var result = calc.Calculate(SpecScenario_Yellow(salePrice: 70m));

        result.NetProfit.Amount.Should().BeLessThan(0m);
        result.Status.Should().Be(ProfitabilityStatus.Red);
    }

    [Fact]
    public void HighMargin_Yields_Green()
    {
        // Price=200 keeps CMV=50, commission=12%, fee=5, shipping=18, tax=6%.
        //   cost = 50 + 5 + 18 + 200*0.12 + 200*0.06 = 109 → profit = 91 → margin 45.5%.
        var calc = new ProfitabilityCalculatorService();
        var result = calc.Calculate(SpecScenario_Yellow(salePrice: 200m));

        result.Status.Should().Be(ProfitabilityStatus.Green);
        result.NetMarginPercentage.Value.Should().BeGreaterThan(12m);
    }

    [Fact]
    public void MinimumPriceFloor_At_Zero_Floor_Equals_BreakEven()
    {
        // With floor = 0%, the floor price is exactly where profit is zero.
        var calc = new ProfitabilityCalculatorService();
        var floor = ProfitabilityCalculatorService.ComputeMinimumPriceFloor(SpecScenario_Yellow());

        // Feeding the floor back in must yield ~0 profit and Red status (since Yellow threshold is 6%).
        var inputsAtFloor = SpecScenario_Yellow() with { SalePrice = floor };
        var atFloor = calc.Calculate(inputsAtFloor);

        atFloor.NetProfit.Amount.Should().BeApproximately(0m, 0.01m);
        atFloor.Status.Should().Be(ProfitabilityStatus.Red);
    }

    [Fact]
    public void ClassifyStatus_HonorsBoundaries()
    {
        // Green boundary: margin == Green → Green.
        ProfitabilityCalculatorService
            .ClassifyStatus(new Percentage(12m), StandardThresholds)
            .Should().Be(ProfitabilityStatus.Green);

        // Yellow boundary: margin == Yellow → Yellow.
        ProfitabilityCalculatorService
            .ClassifyStatus(new Percentage(6m), StandardThresholds)
            .Should().Be(ProfitabilityStatus.Yellow);

        // Just below Yellow → Red.
        ProfitabilityCalculatorService
            .ClassifyStatus(new Percentage(5.99m), StandardThresholds)
            .Should().Be(ProfitabilityStatus.Red);
    }
}
