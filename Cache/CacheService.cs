using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using GithubNote.NET.Services.Performance;

namespace GithubNote.NET.Cache
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, string> _keys;
        private readonly CacheOptions _options;
        private readonly PerformanceMonitor _performanceMonitor;

        public CacheService(
            IMemoryCache cache,
            IOptions<CacheOptions> options,
            PerformanceMonitor performanceMonitor)
        {
            _cache = cache;
            _options = options.Value;
            _performanceMonitor = performanceMonitor;
            _keys = new ConcurrentDictionary<string, string>();
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var value = _cache.Get<T>(key);
            if (value != null)
            {
                _performanceMonitor.TrackCacheHit();
            }
            else
            {
                _performanceMonitor.TrackCacheMiss();
            }
            return await Task.FromResult(value);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expirationTime ?? _options.DefaultExpiration);

            _cache.Set(key, value, options);
            _keys.TryAdd(key, string.Empty);
            
            if (value != null)
            {
                var size = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value).Length;
                _performanceMonitor.TrackMemoryUsage($"Cache_{key}", size);
            }
            
            await Task.CompletedTask;
        }

        public async Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await Task.FromResult(_cache.TryGetValue(key, out _));
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expirationTime = null)
        {
            if (await ExistsAsync(key))
            {
                return await GetAsync<T>(key);
            }

            var value = await factory();
            await SetAsync(key, value, expirationTime);
            return value;
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            var keys = _keys.Keys.Where(k => k.StartsWith(prefix));
            foreach (var key in keys)
            {
                await RemoveAsync(key);
            }
        }

        public async Task ClearAsync()
        {
            foreach (var key in _keys.Keys)
            {
                await RemoveAsync(key);
            }
        }
    }
}
