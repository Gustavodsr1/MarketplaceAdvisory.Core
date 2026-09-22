using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.Integrations.Abstractions.Models;

namespace MarketplaceAdvisory.Core.Infrastructure.Integrations;

/// <summary>
/// Amazon anti-corruption adapter. Implementation is pending.
/// </summary>
internal sealed class AmazonAdapter : IMarketplaceAdapter
{
    public MarketplaceType Marketplace => MarketplaceType.Amazon;

    public Task<IReadOnlyList<CanonicalOrder>> GetOrdersAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Amazon order synchronization is not implemented yet.");

    public Task<CanonicalListing> PublishListingAsync(CanonicalListing listing, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Amazon listing publishing is not implemented yet.");
}
