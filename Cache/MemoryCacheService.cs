using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GithubNote.NET.Cache
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, string> _keys;
        private readonly MemoryCacheOptions _options;

        public MemoryCacheService(IOptions<MemoryCacheOptions> options)
        {
            _options = options.Value;
            _cache = new MemoryCache(options);
            _keys = new ConcurrentDictionary<string, string>();
        }

        public async Task<T> GetAsync<T>(string key)
        {
            return await Task.FromResult(_cache.Get<T>(key));
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expirationTime.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expirationTime;
            }
            else
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(30);
            }

            options.RegisterPostEvictionCallback((k, v, r, s) =>
            {
                _keys.TryRemove(k.ToString(), out _);
            });

            _cache.Set(key, value, options);
            _keys.TryAdd(key, key);
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
            var keys = _keys.Keys.Where(k => k.StartsWith(prefix)).ToList();
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
