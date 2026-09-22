using MarketplaceAdvisory.Integrations.Abstractions.Models;

namespace MarketplaceAdvisory.Integrations.Abstractions;

/// <summary>
/// Anti-Corruption Layer port implemented once per marketplace. Credentials are always
/// passed explicitly (never resolved from ambient state) to keep tenant isolation strict.
/// </summary>
public interface IMarketplaceAdapter
{
    MarketplaceType Marketplace { get; }

    Task<IReadOnlyList<CanonicalOrder>> GetOrdersAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<CanonicalListing> PublishListingAsync(
        CanonicalListing listing,
        CancellationToken cancellationToken);
}
