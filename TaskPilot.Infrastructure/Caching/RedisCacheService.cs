using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TaskPilot.Application.Interfaces.Infrastructure.Caching;

namespace TaskPilot.Infrastructure.Caching;

public class RedisCacheService (IDistributedCache distributedCache): ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var cachedValue = await distributedCache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(cachedValue))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(cachedValue, JsonOptions);

    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var serializedValue = JsonSerializer.Serialize(value, JsonOptions);

        await distributedCache.SetStringAsync(
            key,
            serializedValue,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        return distributedCache.RemoveAsync(key, cancellationToken);
    }
}