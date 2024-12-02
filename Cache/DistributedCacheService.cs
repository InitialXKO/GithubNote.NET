using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace GithubNote.NET.Cache
{
    public class DistributedCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _defaultOptions;

        public DistributedCacheService(
            IDistributedCache cache,
            IOptions<DistributedCacheEntryOptions>? options = default)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _defaultOptions = options?.Value ?? new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            };
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var value = await _cache.GetStringAsync(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            var options = new DistributedCacheEntryOptions();
            
            if (expirationTime.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expirationTime;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = _defaultOptions.AbsoluteExpirationRelativeToNow;
            }

            var jsonValue = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, jsonValue, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _cache.GetAsync(key) != null;
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expirationTime = null)
        {
            var value = await GetAsync<T>(key);
            if (value != null)
            {
                return value;
            }

            value = await factory();
            await SetAsync(key, value, expirationTime);
            return value;
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            // 注意：分布式缓存可能不支持按前缀删除
            // 这里需要具体缓存提供程序的支持
            throw new NotImplementedException("RemoveByPrefix is not supported in distributed cache");
        }

        public async Task ClearAsync()
        {
            // 注意：分布式缓存可能不支持清空操作
            // 这里需要具体缓存提供程序的支持
            throw new NotImplementedException("Clear is not supported in distributed cache");
        }
    }
}
