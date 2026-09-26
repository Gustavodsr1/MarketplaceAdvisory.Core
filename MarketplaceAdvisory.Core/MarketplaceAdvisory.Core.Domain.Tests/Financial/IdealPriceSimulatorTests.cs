using FluentAssertions;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Tests.Financial;

public sealed class IdealPriceSimulatorTests
{
    private static ProfitabilityInputs BaseInputs =>
        new(
            MarketplaceType.MercadoLivre,
            Cmv: new Money(50m, "BRL"),
            SalePrice: new Money(100m, "BRL"),
            CommissionPercentage: new Percentage(12m),
            FixedUnitFee: new Money(5m, "BRL"),
            SubsidizedShippingCost: new Money(18m, "BRL"),
            TaxRate: new Percentage(6m),
            Regime: TaxRegime.SimplesNacional,
            Thresholds: new ProfitabilityThresholds(new Percentage(12m), new Percentage(6m), new Percentage(0m)));

    [Fact]
    public void SolveForTargetMargin_Reversed_YieldsRequestedMargin()
    {
        // Spec Scenario 3: target 15% net margin — recomputing at the suggested price must yield 15% ± 0.05pp.
        var simulator = new IdealPriceSimulator();
        var calc = new ProfitabilityCalculatorService();
        var target = new Percentage(15m);

        var suggested = simulator.SolveForTargetMargin(BaseInputs, target);
        var recomputed = calc.Calculate(BaseInputs with { SalePrice = suggested });

        recomputed.NetMarginPercentage.Value.Should().BeApproximately(15m, 0.05m);
    }

    [Fact]
    public void SolveForTargetMargin_ThrowsWhenTargetConsumesFullPrice()
    {
        var simulator = new IdealPriceSimulator();
        // 90% target + 12% commission + 6% tax > 100% — impossible.
        Action act = () => simulator.SolveForTargetMargin(BaseInputs, new Percentage(90m));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SolveWithTierResolver_ConvergesWithTierSwitch()
    {
        // Emulate a shipping table where any price above R$120 pays R$25 instead of R$18.
        var simulator = new IdealPriceSimulator();
        var calc = new ProfitabilityCalculatorService();

        Money Resolver(Money price) =>
            price.Amount > 120m ? new Money(25m, "BRL") : new Money(18m, "BRL");

        var suggested = simulator.SolveWithTierResolver(
            BaseInputs, new Percentage(15m), Resolver);

        var finalShipping = Resolver(suggested);
        var recomputed = calc.Calculate(
            BaseInputs with { SubsidizedShippingCost = finalShipping, SalePrice = suggested });

        recomputed.NetMarginPercentage.Value.Should().BeApproximately(15m, 0.05m);
    }
}
