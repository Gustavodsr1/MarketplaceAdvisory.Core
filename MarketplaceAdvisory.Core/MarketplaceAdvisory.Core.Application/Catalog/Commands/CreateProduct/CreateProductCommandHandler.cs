using ErrorOr;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Application.Common.Messaging;
using MarketplaceAdvisory.Contracts.Catalog;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Catalog.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : ICommandHandler<CreateProductCommand, ErrorOr<ProductDto>>
{
    public async Task<ErrorOr<ProductDto>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? TenantId.From(Guid.Empty);

        var salePrice = new Money(request.Price, request.Currency);
        var cmv = new Money(Math.Max(salePrice.Amount * 0.5m, 0.01m), salePrice.Currency);

        var result = Product.Create(
            tenantId,
            request.Sku,
            request.Name,
            cmv,
            salePrice,
            new Weight(100),
            new Dimensions(10m, 10m, 10m));

        if (result.IsError)
        {
            return result.Errors;
        }

        var product = result.Value;

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProductDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Price.Amount,
            product.Price.Currency);
    }
}
