using ErrorOr;
using MarketplaceAdvisory.Contracts.Financial;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Application.Common.Messaging;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Financial.Commands.SetSalePrice;

public sealed class SetSalePriceCommandHandler(
    IProductRepository products,
    IApplicationDbContext dbContext,
    IProfitabilityInputsAssembler assembler,
    IProfitabilityFloorGuard floorGuard,
    ITenantContext tenantContext,
    ProfitabilityCalculatorService calculator)
    : ICommandHandler<SetSalePriceCommand, ErrorOr<ProductProfitabilityDto>>
{
    public async Task<ErrorOr<ProductProfitabilityDto>> Handle(
        SetSalePriceCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MarketplaceType>(request.Marketplace, ignoreCase: true, out var marketplace))
        {
            return Error.Validation("SalePrice.Marketplace", $"Unknown marketplace '{request.Marketplace}'.");
        }

        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || IsOutsideTenant(product, tenantContext))
        {
            return Error.NotFound("Product.NotFound", $"Product '{request.ProductId}' was not found.");
        }

        var proposed = new Money(request.Amount, request.Currency);

        var inputs = await assembler.AssembleAsync(product, marketplace, proposed, cancellationToken);
        if (inputs.IsError)
        {
            return inputs.Errors;
        }

        var overrideToken = request.Override is null
            ? null
            : new HumanOverrideToken(
                actorId: tenantContext.TenantId?.ToString() ?? "unknown",
                reasonCode: request.Override.ReasonCode,
                acceptedLossAmount: request.Override.AcceptedLossAmount);

        var decision = await floorGuard.EvaluateAsync(inputs.Value, overrideToken, cancellationToken);

        // Principle VIII: translate a held write into the two documented API outcomes.
        if (decision is FloorDecision.HeldFloorHit held)
        {
            return overrideToken is null
                ? Error.Conflict(
                    "floor.override_required",
                    $"Sale price {proposed.Amount} {proposed.Currency} is below the minimum floor " +
                    $"{held.Floor.Amount} {held.Floor.Currency}. Resubmit with an override token to accept the loss.")
                : Error.Conflict(
                    "floor.hit",
                    $"Declared accepted loss ({request.Override!.AcceptedLossAmount}) is smaller than the actual " +
                    $"loss ({held.Floor.Amount - proposed.Amount}) to reach the floor.");
        }

        var applied = product.SetSalePrice(marketplace, proposed, decision, overrideToken);
        if (applied.IsError)
        {
            return applied.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var result = calculator.Calculate(inputs.Value);
        return FinancialMappings.ToProfitabilityDto(marketplace, proposed, result);
    }

    private static bool IsOutsideTenant(Product product, ITenantContext tenantContext) =>
        tenantContext.TenantId is { } tenant && product.TenantId != tenant;
}
