using MarketplaceAdvisory.Core.Domain.Common;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Immutable audit record of a single profitability calculation. Written every time inputs
/// change so history is fully reconstructable — supports Principle VIII (Profitability
/// Guardian) and SC-008 (traceability of every price change).
/// </summary>
public sealed class ProfitabilityCalculation : AggregateRoot<Guid>
{
    private ProfitabilityCalculation(
        Guid id,
        TenantId tenantId,
        Guid productId,
        MarketplaceType marketplace,
        DateTimeOffset computedAt,
        ProfitabilityInputs inputsSnapshot,
        Money netProfit,
        Percentage netMarginPercentage,
        ProfitabilityStatus status,
        Money minimumPriceFloor,
        CalculationTrigger causedBy)
        : base(id, tenantId)
    {
        ProductId = productId;
        Marketplace = marketplace;
        ComputedAt = computedAt;
        InputsSnapshot = inputsSnapshot;
        NetProfit = netProfit;
        NetMarginPercentage = netMarginPercentage;
        Status = status;
        MinimumPriceFloor = minimumPriceFloor;
        CausedBy = causedBy;
    }

    private ProfitabilityCalculation() : base(Guid.Empty, default)
    {
        InputsSnapshot = default!;
        NetProfit = Money.Zero("BRL");
        NetMarginPercentage = Percentage.Zero;
        MinimumPriceFloor = Money.Zero("BRL");
    }

    public Guid ProductId { get; private set; }

    public MarketplaceType Marketplace { get; private set; }

    public DateTimeOffset ComputedAt { get; private set; }

    public ProfitabilityInputs InputsSnapshot { get; private set; }

    public Money NetProfit { get; private set; }

    public Percentage NetMarginPercentage { get; private set; }

    public ProfitabilityStatus Status { get; private set; }

    public Money MinimumPriceFloor { get; private set; }

    public CalculationTrigger CausedBy { get; private set; }

    public static ProfitabilityCalculation Materialize(
        TenantId tenantId,
        Guid productId,
        DateTimeOffset computedAt,
        ProfitabilityInputs inputsSnapshot,
        Money netProfit,
        Percentage netMarginPercentage,
        ProfitabilityStatus status,
        Money minimumPriceFloor,
        CalculationTrigger causedBy) =>
        new(
            Guid.NewGuid(),
            tenantId,
            productId,
            inputsSnapshot.Marketplace,
            computedAt,
            inputsSnapshot,
            netProfit,
            netMarginPercentage,
            status,
            minimumPriceFloor,
            causedBy);
}
