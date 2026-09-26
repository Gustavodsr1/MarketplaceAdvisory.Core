using FluentAssertions;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Tests;

public sealed class ValueObjectTests
{
    [Fact]
    public void Weight_Zero_Throws()
    {
        Action act = () => new Weight(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Weight_Addition_SumsGrams()
    {
        var total = new Weight(300) + new Weight(200);
        total.Grams.Should().Be(500);
    }

    [Fact]
    public void Dimensions_VolumetricWeight_ComputesGrams()
    {
        // 20 * 15 * 10 = 3000 cm³; divided by 6000 = 0.5 kg = 500 g.
        var dims = new Dimensions(20m, 15m, 10m);
        dims.VolumetricWeightGrams(6000).Should().Be(500);
    }

    [Fact]
    public void Dimensions_CombineForBundle_MaxMaxSumRule()
    {
        // From spec.md example: X (20×15×10) + Y (12×8×5)
        //   sides sorted per box: X = [20,15,10], Y = [12,8,5]
        //   max of largest sides  = max(20,12) = 20
        //   max of middle sides   = max(15,8)  = 15
        //   sum of smallest sides = 10+5       = 15
        var x = new Dimensions(20m, 15m, 10m);
        var y = new Dimensions(12m, 8m, 5m);
        var combined = Dimensions.CombineForBundle(new[] { x, y });

        combined.WidthCm.Should().Be(20m);
        combined.HeightCm.Should().Be(15m);
        combined.LengthCm.Should().Be(15m);
    }

    [Fact]
    public void Percentage_OutOfRange_Throws()
    {
        Action low = () => new Percentage(-100.01m);
        Action high = () => new Percentage(100.01m);
        low.Should().Throw<ArgumentOutOfRangeException>();
        high.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Percentage_AllowsNegative_ForLossMargins()
    {
        // Selling at a loss is a valid business case; the guard blocks it but the math must
        // still be able to represent the resulting negative margin.
        var loss = new Percentage(-22.28m);
        loss.Value.Should().Be(-22.28m);
    }

    [Fact]
    public void ProfitabilityThresholds_ViolatesOrdering_Throws()
    {
        Action act = () => new ProfitabilityThresholds(
            new Percentage(5m), // Green < Yellow — invalid
            new Percentage(10m),
            new Percentage(0m));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProfitabilityThresholds_ValidOrdering_Succeeds()
    {
        var thresholds = new ProfitabilityThresholds(
            new Percentage(12m),
            new Percentage(6m),
            new Percentage(0m));

        thresholds.Green.Value.Should().Be(12m);
        thresholds.Yellow.Value.Should().Be(6m);
        thresholds.MinimumFloor.Value.Should().Be(0m);
    }
}
