using System;

namespace GithubNote.NET.Services.Performance
{
    public class PerformanceThresholds
    {
        public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromSeconds(2);
        public long MaxMemoryUsage { get; set; } = 1024 * 1024 * 512; // 512MB
        public int MaxActiveConnections { get; set; } = 100;
        public int MinCacheHitRate { get; set; } = 70; // 70%
    }
}
