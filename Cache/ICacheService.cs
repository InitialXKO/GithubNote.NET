using System;
using System.Threading.Tasks;

namespace GithubNote.NET.Cache
{
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expirationTime = null);
        Task RemoveByPrefixAsync(string prefix);
        Task ClearAsync();
    }
}
