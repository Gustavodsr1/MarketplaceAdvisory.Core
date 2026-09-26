using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Financial;

/// <summary>
/// Concrete <see cref="IProfitabilityFloorGuard"/>. Delegates to the pure Domain calculator so
/// the enforcement point is the same math the seller sees on the dashboard — no drift possible.
/// </summary>
public sealed class ProfitabilityFloorGuard : IProfitabilityFloorGuard
{
    private readonly ProfitabilityCalculatorService _calculator;

    public ProfitabilityFloorGuard(ProfitabilityCalculatorService calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    public Task<FloorDecision> EvaluateAsync(
        ProfitabilityInputs inputs,
        HumanOverrideToken? overrideToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var floor = ProfitabilityCalculatorService.ComputeMinimumPriceFloor(inputs);
        var proposed = inputs.SalePrice;

        FloorDecision decision;
        if (proposed.Amount >= floor.Amount)
        {
            decision = new FloorDecision.Allowed(proposed);
        }
        else
        {
            var diff = floor.Amount - proposed.Amount;

            // An override is honored only when the human declared a loss at least as large as the
            // real gap to the floor — under-declaring the loss is treated as a plain floor hit.
            decision = overrideToken is not null && overrideToken.AcceptedLossAmount >= diff
                ? new FloorDecision.RequiresOverride(floor, proposed, diff)
                : new FloorDecision.HeldFloorHit(floor, proposed);
        }

        return Task.FromResult(decision);
    }
}
