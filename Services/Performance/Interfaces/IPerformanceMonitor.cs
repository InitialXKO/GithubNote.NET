using System;
using System.Threading.Tasks;
using GithubNote.NET.Services.Performance.Models;

namespace GithubNote.NET.Services.Performance.Interfaces
{
    public interface IPerformanceMonitor
    {
        Task TrackOperationAsync(string context, TimeSpan responseTime);
        Task TrackMemoryUsageAsync(string context, long memoryUsage);
        Task<PerformanceMetrics> GetMetricsAsync();
        Task<CacheStatistics> GetCacheStatisticsAsync();
        Task<int> GetActiveConnectionsAsync();
    }
}
