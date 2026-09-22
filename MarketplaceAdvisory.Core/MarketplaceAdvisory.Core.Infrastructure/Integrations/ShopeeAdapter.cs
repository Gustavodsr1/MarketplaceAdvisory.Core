using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.Integrations.Abstractions.Models;

namespace MarketplaceAdvisory.Core.Infrastructure.Integrations;

/// <summary>
/// Shopee anti-corruption adapter. Implementation is pending.
/// </summary>
internal sealed class ShopeeAdapter : IMarketplaceAdapter
{
    public MarketplaceType Marketplace => MarketplaceType.Shopee;

    public Task<IReadOnlyList<CanonicalOrder>> GetOrdersAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Shopee order synchronization is not implemented yet.");

    public Task<CanonicalListing> PublishListingAsync(CanonicalListing listing, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Shopee listing publishing is not implemented yet.");
}
