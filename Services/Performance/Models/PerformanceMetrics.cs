using System;
using System.Collections.Generic;

namespace GithubNote.NET.Services.Performance.Models
{
    public class PerformanceMetrics
    {
        public TimeSpan AverageResponseTime { get; set; }
        public long MemoryUsage { get; set; }
        public Dictionary<string, double> AverageOperationDurations { get; set; }
        public Dictionary<string, long> MemoryUsageByContext { get; set; }
        public double CacheHitRate { get; set; }
        public long TotalMemoryUsed { get; set; }
        public long CacheHitCount { get; set; }
        public long CacheMissCount { get; set; }
        public DateTime LastUpdated { get; set; }

        public PerformanceMetrics()
        {
            AverageOperationDurations = new Dictionary<string, double>();
            MemoryUsageByContext = new Dictionary<string, long>();
            LastUpdated = DateTime.UtcNow;
        }
    }
}
