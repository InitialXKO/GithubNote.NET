using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GithubNote.NET.Services.Performance
{
    public interface IPerformanceOptimizer
    {
        Task OptimizeCacheStrategyAsync();
        Task<bool> ShouldPreloadDataAsync(string context);
        Task UpdateOptimizationSettingsAsync(OptimizationSettings settings);
        Task<OptimizationSettings> GetCurrentSettingsAsync();
    }

    public class OptimizationSettings
    {
        public int MaxCacheSize { get; set; }
        public TimeSpan DefaultCacheExpiration { get; set; }
        public bool EnablePreloading { get; set; }
        public int PreloadBatchSize { get; set; }
        public Dictionary<string, bool> ContextPreloadSettings { get; set; }
    }
}
