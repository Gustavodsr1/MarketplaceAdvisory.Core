using System.Text.Json;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace MarketplaceAdvisory.Core.Infrastructure.Cache;

/// <summary>
/// Redis-backed distributed cache implementation used for read caching and webhook idempotency.
/// </summary>
public sealed class RedisCacheService(IDistributedCache distributedCache) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var payload = await distributedCache.GetAsync(key, cancellationToken);
        return payload is null ? default : JsonSerializer.Deserialize<T>(payload, SerializerOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? timeToLive, CancellationToken cancellationToken)
    {
        var options = new DistributedCacheEntryOptions();
        if (timeToLive.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = timeToLive;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        await distributedCache.SetAsync(key, payload, options, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
        distributedCache.RemoveAsync(key, cancellationToken);
}
