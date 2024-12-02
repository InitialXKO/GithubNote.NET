using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GithubNote.NET.Services.Performance
{
    public class CacheManager : ICacheManager
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheManager> _logger;
        private readonly CacheStatistics _statistics;

        public CacheManager(IMemoryCache cache, ILogger<CacheManager> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statistics = new CacheStatistics();
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            _statistics.TotalRequests++;

            if (_cache.TryGetValue(key, out T? value))
            {
                _statistics.Hits++;
                _logger.LogDebug($"Cache hit for key: {key}");
                await Task.CompletedTask; // For async consistency
                return value;
            }

            _statistics.Misses++;
            _logger.LogDebug($"Cache miss for key: {key}");
            return null;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var cacheEntryOptions = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                cacheEntryOptions.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                cacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(30);
            }

            _cache.Set(key, value, cacheEntryOptions);
            _statistics.ItemCount++;
            
            _logger.LogDebug($"Added item to cache with key: {key}");
            await Task.CompletedTask; // For async consistency
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            _cache.Remove(key);
            _statistics.ItemCount = Math.Max(0, _statistics.ItemCount - 1);
            
            _logger.LogDebug($"Removed item from cache with key: {key}");
            await Task.CompletedTask; // For async consistency
        }

        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            await Task.CompletedTask; // For async consistency
            return _statistics;
        }

        public async Task ClearAsync()
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
            }
            
            _statistics.ItemCount = 0;
            _statistics.TotalRequests = 0;
            _statistics.Hits = 0;
            _statistics.Misses = 0;
            
            _logger.LogInformation("Cache cleared");
            await Task.CompletedTask; // For async consistency
        }
    }
}
