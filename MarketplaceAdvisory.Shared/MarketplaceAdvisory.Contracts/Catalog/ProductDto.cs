namespace MarketplaceAdvisory.Contracts.Catalog;

/// <summary>
/// Product projection returned by Core.Api and consumed by the BFF/front-end.
/// </summary>
public sealed record ProductDto(Guid Id, string Sku, string Name, decimal Price, string Currency);
