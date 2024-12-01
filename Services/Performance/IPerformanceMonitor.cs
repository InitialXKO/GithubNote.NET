using System;
using System.Threading.Tasks;

namespace GithubNote.NET.Services.Performance
{
    public interface IPerformanceMonitor
    {
        void TrackOperation(string operationName, TimeSpan duration);
        void TrackMemoryUsage(string context, long bytesUsed);
        Task<PerformanceMetrics> GetMetricsAsync();
        void ResetMetrics();
    }

    public class PerformanceMetrics
    {
        public Dictionary<string, TimeSpan> AverageOperationDurations { get; set; }
        public Dictionary<string, long> MemoryUsageByContext { get; set; }
        public long TotalMemoryUsed { get; set; }
        public int CacheHitCount { get; set; }
        public int CacheMissCount { get; set; }
    }
}
