namespace MarketplaceAdvisory.Core.Application.Common.Abstractions;

/// <summary>
/// Distributed cache abstraction (implemented with Redis in Infrastructure).
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);

    Task SetAsync<T>(string key, T value, TimeSpan? timeToLive, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
