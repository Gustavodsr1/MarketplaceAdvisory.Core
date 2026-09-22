using MarketplaceAdvisory.Core.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceAdvisory.Core.Application.Common.Abstractions;

/// <summary>
/// Write-side database abstraction (EF Core) used by command handlers. Exposing DbSets here
/// is a deliberate CQRS-lite trade-off that keeps command handlers persistence-aware but
/// provider-agnostic.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
