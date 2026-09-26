using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Deterministic Financial Engine. No I/O, no framework references, no clock beyond the one
/// injected by the caller — every rule is pure so tests are golden and cheap.
///
/// Implements the formulas in the spec Financial Engine §Rules A1 (net profit / margin), A2
/// (traffic-light classification) and A4 (minimum-price floor).
/// </summary>
public sealed class ProfitabilityCalculatorService
{
    /// <summary>
    /// Rule A1 — computes net profit, net margin and the minimum price floor for a set of
    /// inputs. Never rounds intermediate values; only the reported margin is rounded to keep
    /// UI display stable.
    /// </summary>
    public ProfitabilityResult Calculate(ProfitabilityInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var currency = inputs.SalePrice.Currency;

        // Commission and tax are proportional to the sale price.
        var commissionAmount = inputs.SalePrice.Amount * inputs.CommissionPercentage.AsFraction;
        var taxAmount = inputs.SalePrice.Amount * inputs.TaxRate.AsFraction;

        var totalCostAtCurrentPrice =
            inputs.Cmv.Amount
            + inputs.FixedUnitFee.Amount
            + inputs.SubsidizedShippingCost.Amount
            + commissionAmount
            + taxAmount;

        var netProfitAmount = inputs.SalePrice.Amount - totalCostAtCurrentPrice;

        var marginFraction = inputs.SalePrice.Amount == 0m
            ? 0m
            : netProfitAmount / inputs.SalePrice.Amount;

        var netMargin = new Percentage(Math.Round(marginFraction * 100m, 4));
        var status = ClassifyStatus(netMargin, inputs.Thresholds);
        var minimumFloor = ComputeMinimumPriceFloor(inputs);

        return new ProfitabilityResult(
            NetProfit: new Money(Math.Round(netProfitAmount, 4), currency),
            NetMarginPercentage: netMargin,
            Status: status,
            MinimumPriceFloor: minimumFloor);
    }

    /// <summary>
    /// Rule A2 — traffic-light classification. Green when margin ≥ Green threshold, Yellow
    /// when margin ≥ Yellow, Red otherwise (including negative margins).
    /// </summary>
    public static ProfitabilityStatus ClassifyStatus(Percentage marginPct, ProfitabilityThresholds thresholds)
    {
        if (marginPct >= thresholds.Green) return ProfitabilityStatus.Green;
        if (marginPct >= thresholds.Yellow) return ProfitabilityStatus.Yellow;
        return ProfitabilityStatus.Red;
    }

    /// <summary>
    /// Rule A4 — solves the sale price at which margin equals the tenant's minimum floor
    /// percentage, given fixed costs and proportional (commission + tax) fractions.
    ///
    /// Derivation (all amounts positive):
    ///   margin/100 = (P - Cmv - F - S - c·P - t·P) / P
    ///   ⇒ P (1 - m - c - t) = Cmv + F + S
    ///   ⇒ P = (Cmv + F + S) / (1 - m - c - t)
    /// The current shipping tier is honored by the caller; the floor uses the inputs as-is.
    /// </summary>
    public static Money ComputeMinimumPriceFloor(ProfitabilityInputs inputs)
    {
        var fixedCost =
            inputs.Cmv.Amount + inputs.FixedUnitFee.Amount + inputs.SubsidizedShippingCost.Amount;

        var proportionalConsumed =
            inputs.CommissionPercentage.AsFraction
            + inputs.TaxRate.AsFraction
            + inputs.Thresholds.MinimumFloor.AsFraction;

        if (proportionalConsumed >= 1m)
        {
            // Impossible to reach the floor at any price — mathematically unbounded. Return the
            // maximum representable price so downstream guard rejects any write.
            return new Money(decimal.MaxValue, inputs.SalePrice.Currency);
        }

        var floor = fixedCost / (1m - proportionalConsumed);
        return new Money(Math.Round(floor, 4, MidpointRounding.AwayFromZero), inputs.SalePrice.Currency);
    }
}

/// <summary>
/// Result of a single calculation. Does not include the persistence identifiers so it can be
/// consumed by both the audit trail (via <see cref="ProfitabilityCalculation.Materialize"/>) and
/// the simulator without incidental coupling.
/// </summary>
public sealed record ProfitabilityResult(
    Money NetProfit,
    Percentage NetMarginPercentage,
    ProfitabilityStatus Status,
    Money MinimumPriceFloor);
