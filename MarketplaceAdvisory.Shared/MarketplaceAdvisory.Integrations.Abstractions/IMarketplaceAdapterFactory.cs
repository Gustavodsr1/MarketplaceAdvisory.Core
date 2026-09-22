namespace MarketplaceAdvisory.Integrations.Abstractions;

/// <summary>
/// Resolves the correct <see cref="IMarketplaceAdapter"/> for a given marketplace.
/// Implemented in Infrastructure using the Strategy pattern.
/// </summary>
public interface IMarketplaceAdapterFactory
{
    IMarketplaceAdapter For(MarketplaceType marketplace);
}
