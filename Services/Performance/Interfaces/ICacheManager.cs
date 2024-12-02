using System;
using System.Threading.Tasks;

namespace GithubNote.NET.Services.Performance
{
    public interface ICacheManager
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task<CacheStatistics> GetStatisticsAsync();
        Task ClearAsync();
    }
}
