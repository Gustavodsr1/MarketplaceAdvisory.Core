using MarketplaceAdvisory.Integrations.Abstractions;

namespace MarketplaceAdvisory.Core.Infrastructure.Integrations;

/// <summary>
/// Strategy-based factory that resolves the correct marketplace adapter from the set of
/// registered adapters. Each marketplace keeps an isolated failure domain.
/// </summary>
internal sealed class MarketplaceAdapterFactory : IMarketplaceAdapterFactory
{
    private readonly IReadOnlyDictionary<MarketplaceType, IMarketplaceAdapter> _adapters;

    public MarketplaceAdapterFactory(IEnumerable<IMarketplaceAdapter> adapters) =>
        _adapters = adapters.ToDictionary(adapter => adapter.Marketplace);

    public IMarketplaceAdapter For(MarketplaceType marketplace) =>
        _adapters.TryGetValue(marketplace, out var adapter)
            ? adapter
            : throw new NotSupportedException($"No adapter is registered for marketplace '{marketplace}'.");
}
