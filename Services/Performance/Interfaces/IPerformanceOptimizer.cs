using System;
using System.Threading.Tasks;
using GithubNote.NET.Services.Performance.Models;

namespace GithubNote.NET.Services.Performance.Interfaces
{
    public interface IPerformanceOptimizer
    {
        Task<bool> ShouldOptimizeAsync(string context);
        Task RecordMetricAsync(string context, TimeSpan responseTime, long memoryUsage);
        Task<PerformanceMetrics> GetMetricsAsync(string context);
        void SetThresholds(PerformanceThresholds thresholds);
    }
}
