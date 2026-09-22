using MarketplaceAdvisory.Core.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceAdvisory.Core.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(ApplicationDbContext dbContext) : IProductRepository
{
    public async Task AddAsync(Product product, CancellationToken cancellationToken) =>
        await dbContext.Products.AddAsync(product, cancellationToken);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
}
