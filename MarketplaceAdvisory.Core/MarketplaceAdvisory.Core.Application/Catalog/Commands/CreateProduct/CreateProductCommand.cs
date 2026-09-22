using ErrorOr;
using MarketplaceAdvisory.Core.Application.Common.Messaging;
using MarketplaceAdvisory.Contracts.Catalog;

namespace MarketplaceAdvisory.Core.Application.Catalog.Commands.CreateProduct;

public sealed record CreateProductCommand(string Sku, string Name, decimal Price, string Currency)
    : ICommand<ErrorOr<ProductDto>>;
