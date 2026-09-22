using ErrorOr;
using MarketplaceAdvisory.Core.Application.Common.Messaging;
using MarketplaceAdvisory.Contracts.Catalog;

namespace MarketplaceAdvisory.Core.Application.Catalog.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ErrorOr<ProductDto>>;
