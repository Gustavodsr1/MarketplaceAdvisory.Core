using ErrorOr;
using MarketplaceAdvisory.Contracts.Financial;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Application.Common.Messaging;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Financial.Queries.RunIdealPriceSimulator;

public sealed class RunIdealPriceSimulatorQueryHandler(
    IProductRepository products,
    IProfitabilityInputsAssembler assembler,
    ITenantContext tenantContext,
    IdealPriceSimulator simulator,
    ProfitabilityCalculatorService calculator)
    : IQueryHandler<RunIdealPriceSimulatorQuery, ErrorOr<SimulatorResponseDto>>
{
    public async Task<ErrorOr<SimulatorResponseDto>> Handle(
        RunIdealPriceSimulatorQuery request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MarketplaceType>(request.Marketplace, ignoreCase: true, out var marketplace))
        {
            return Error.Validation("Simulator.Marketplace", $"Unknown marketplace '{request.Marketplace}'.");
        }

        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || IsOutsideTenant(product, tenantContext))
        {
            return Error.NotFound("Product.NotFound", $"Product '{request.ProductId}' was not found.");
        }

        // Seed at the product's current price so the assembler resolves the current shipping tier.
        var seed = await assembler.AssembleAsync(product, marketplace, product.Price, cancellationToken);
        if (seed.IsError)
        {
            return seed.Errors;
        }

        Money suggested;
        try
        {
            suggested = simulator.SolveForTargetMargin(seed.Value, new Percentage(request.TargetNetMarginPercent));
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("Simulator.Target", ex.Message);
        }

        // Recompute at the suggested price so the reported margin/floor reflect that exact price
        // (and any shipping tier the assembler resolves there).
        var finalInputs = await assembler.AssembleAsync(product, marketplace, suggested, cancellationToken);
        if (finalInputs.IsError)
        {
            return finalInputs.Errors;
        }

        var result = calculator.Calculate(finalInputs.Value);

        return new SimulatorResponseDto(
            suggested.ToDto(),
            result.NetMarginPercentage.Value,
            ShippingTierAtSuggestedPrice: null, // tier label lands with the Phase 3 tier catalog
            result.MinimumPriceFloor.ToDto());
    }

    private static bool IsOutsideTenant(Product product, ITenantContext tenantContext) =>
        tenantContext.TenantId is { } tenant && product.TenantId != tenant;
}
