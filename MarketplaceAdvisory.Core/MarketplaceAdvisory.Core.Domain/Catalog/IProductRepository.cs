namespace MarketplaceAdvisory.Core.Domain.Catalog;

/// <summary>
/// Write-side repository port for the <see cref="Product"/> aggregate. Implemented in Infrastructure.
/// </summary>
public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
