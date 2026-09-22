using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.Integrations.Abstractions.Models;

namespace MarketplaceAdvisory.Core.Infrastructure.Integrations;

/// <summary>
/// Mercado Livre anti-corruption adapter. Raw Mercado Livre payloads must be mapped to the
/// canonical models here and never leak outside this folder. Implementation is pending.
/// </summary>
internal sealed class MercadoLivreAdapter : IMarketplaceAdapter
{
    public MarketplaceType Marketplace => MarketplaceType.MercadoLivre;

    public Task<IReadOnlyList<CanonicalOrder>> GetOrdersAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Mercado Livre order synchronization is not implemented yet.");

    public Task<CanonicalListing> PublishListingAsync(CanonicalListing listing, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Mercado Livre listing publishing is not implemented yet.");
}
