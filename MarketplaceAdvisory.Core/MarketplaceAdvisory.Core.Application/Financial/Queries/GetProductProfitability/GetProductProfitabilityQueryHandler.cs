using ErrorOr;
using MarketplaceAdvisory.Contracts.Financial;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Application.Common.Messaging;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.Core.Domain.Financial;

namespace MarketplaceAdvisory.Core.Application.Financial.Queries.GetProductProfitability;

public sealed class GetProductProfitabilityQueryHandler(
    IProductRepository products,
    IProfitabilityInputsAssembler assembler,
    ITenantContext tenantContext,
    ProfitabilityCalculatorService calculator)
    : IQueryHandler<GetProductProfitabilityQuery, ErrorOr<IReadOnlyList<ProductProfitabilityDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<ProductProfitabilityDto>>> Handle(
        GetProductProfitabilityQuery request,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || IsOutsideTenant(product, tenantContext))
        {
            return Error.NotFound("Product.NotFound", $"Product '{request.ProductId}' was not found.");
        }

        var marketplace = product.DefaultMarketplace;
        var inputs = await assembler.AssembleAsync(product, marketplace, product.Price, cancellationToken);
        if (inputs.IsError)
        {
            return inputs.Errors;
        }

        var result = calculator.Calculate(inputs.Value);

        IReadOnlyList<ProductProfitabilityDto> projection =
        [
            FinancialMappings.ToProfitabilityDto(marketplace, product.Price, result)
        ];

        return ErrorOrFactory.From(projection);
    }

    private static bool IsOutsideTenant(Product product, ITenantContext tenantContext) =>
        tenantContext.TenantId is { } tenant && product.TenantId != tenant;
}
