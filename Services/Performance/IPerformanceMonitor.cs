using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GithubNote.NET.Services.Performance.Models;

namespace GithubNote.NET.Services.Performance
{
    public interface IPerformanceMonitor
    {
        Task TrackOperationAsync(string context, TimeSpan responseTime);
        void TrackOperation(string context, TimeSpan duration);
        Task TrackMemoryUsageAsync(string context, long memoryUsage);
        Task<Dictionary<string, double>> GetAverageOperationDurations();
        Task<Dictionary<string, long>> GetMemoryUsageByContext();
        Task<PerformanceReport> GeneratePerformanceReport();
        Task<PerformanceMetrics> GetMetricsAsync();
        Task<int> GetActiveConnectionsAsync();
        void RecordCacheHit();
        void RecordCacheMiss();
        void ResetMetrics();
    }

    public class PerformanceReport
    {
        public Dictionary<string, double> AverageOperationDurations { get; set; }
        public Dictionary<string, long> MemoryUsageByContext { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; }
        public double CacheHitRate { get; set; }
        public DateTime GeneratedAt { get; set; }

        public PerformanceReport()
        {
            AverageOperationDurations = new Dictionary<string, double>();
            MemoryUsageByContext = new Dictionary<string, long>();
            CustomMetrics = new Dictionary<string, double>();
            GeneratedAt = DateTime.UtcNow;
        }
    }
}
