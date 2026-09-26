using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Reverse solver for Rule A3 — "given a target net margin, tell me the sale price".
///
/// The math mirrors <see cref="ProfitabilityCalculatorService.ComputeMinimumPriceFloor"/> but
/// substitutes the tenant's minimum floor by the caller-supplied target margin:
///
///   P = (Cmv + FixedFee + Shipping) / (1 - target - commission - tax)
///
/// The caller is expected to re-run <see cref="ProfitabilityCalculatorService.Calculate"/>
/// against the suggested price so any shipping-tier switch at the new price is reflected. The
/// <see cref="SolveWithTierResolver"/> overload handles the tier-boundary case by iterating.
/// </summary>
public sealed class IdealPriceSimulator
{
    /// <summary>
    /// Single-shot solve — assumes the current shipping tier remains valid at the suggested
    /// price. Suitable when the caller has already isolated a tier.
    /// </summary>
    public Money SolveForTargetMargin(ProfitabilityInputs inputs, Percentage targetMargin)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(targetMargin);

        var fixedCost =
            inputs.Cmv.Amount + inputs.FixedUnitFee.Amount + inputs.SubsidizedShippingCost.Amount;

        var consumed =
            targetMargin.AsFraction
            + inputs.CommissionPercentage.AsFraction
            + inputs.TaxRate.AsFraction;

        if (consumed >= 1m)
        {
            throw new InvalidOperationException(
                "Target margin plus commission and tax exceed the sale price — no valid sale price exists.");
        }

        var price = fixedCost / (1m - consumed);
        return new Money(Math.Round(price, 4, MidpointRounding.AwayFromZero), inputs.SalePrice.Currency);
    }

    /// <summary>
    /// Tier-aware solve. Delegates the "given a candidate price, return the shipping fee I
    /// should use" lookup to the caller. Iterates at most a handful of times (the number of
    /// shipping tiers is small).
    /// </summary>
    public Money SolveWithTierResolver(
        ProfitabilityInputs seedInputs,
        Percentage targetMargin,
        Func<Money, Money> shippingResolverForPrice,
        int maxIterations = 6)
    {
        ArgumentNullException.ThrowIfNull(shippingResolverForPrice);
        var current = seedInputs;
        Money candidate = SolveForTargetMargin(current, targetMargin);
        for (var i = 0; i < maxIterations; i++)
        {
            var shippingAtCandidate = shippingResolverForPrice(candidate);
            if (shippingAtCandidate.Amount == current.SubsidizedShippingCost.Amount)
            {
                return candidate;
            }

            current = current with { SubsidizedShippingCost = shippingAtCandidate };
            candidate = SolveForTargetMargin(current, targetMargin);
        }

        return candidate;
    }
}
