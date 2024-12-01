using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GithubNote.NET.Services.Performance
{
    public class PerformanceMetrics
    {
        public TimeSpan AverageResponseTime { get; set; }
        public long MemoryUsage { get; set; }
        public int ActiveConnections { get; set; }
        public int CacheHitRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class PerformanceThresholds
    {
        public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromSeconds(2);
        public long MaxMemoryUsage { get; set; } = 1024 * 1024 * 512; // 512MB
        public int MaxActiveConnections { get; set; } = 100;
        public int MinCacheHitRate { get; set; } = 70; // 70%
    }

    public interface IPerformanceOptimizer
    {
        Task<bool> ShouldOptimizeAsync(string context);
        Task RecordMetricAsync(string context, TimeSpan responseTime, long memoryUsage);
        Task<PerformanceMetrics> GetMetricsAsync(string context);
        void SetThresholds(PerformanceThresholds thresholds);
    }

    public class PerformanceOptimizer : IPerformanceOptimizer
    {
        private readonly ILogger<PerformanceOptimizer> _logger;
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics;
        private readonly IPerformanceMonitor _monitor;
        private PerformanceThresholds _thresholds;
        private readonly SemaphoreSlim _optimizationLock;

        public PerformanceOptimizer(
            ILogger<PerformanceOptimizer> logger,
            IPerformanceMonitor monitor,
            IOptions<PerformanceThresholds> thresholds)
        {
            _logger = logger;
            _monitor = monitor;
            _metrics = new ConcurrentDictionary<string, PerformanceMetrics>();
            _thresholds = thresholds.Value;
            _optimizationLock = new SemaphoreSlim(1, 1);
        }

        public async Task<bool> ShouldOptimizeAsync(string context)
        {
            if (!_metrics.TryGetValue(context, out var metrics))
            {
                return false;
            }

            // Check if enough time has passed since last optimization
            if (DateTime.UtcNow - metrics.LastUpdated < TimeSpan.FromMinutes(5))
            {
                return false;
            }

            await _optimizationLock.WaitAsync();
            try
            {
                bool shouldOptimize = false;

                // Check response time threshold
                if (metrics.AverageResponseTime > _thresholds.MaxResponseTime)
                {
                    _logger.LogWarning($"Response time threshold exceeded in {context}: {metrics.AverageResponseTime.TotalMilliseconds}ms");
                    shouldOptimize = true;
                }

                // Check memory usage threshold
                if (metrics.MemoryUsage > _thresholds.MaxMemoryUsage)
                {
                    _logger.LogWarning($"Memory usage threshold exceeded in {context}: {metrics.MemoryUsage / (1024 * 1024)}MB");
                    shouldOptimize = true;
                }

                // Check cache hit rate threshold
                if (metrics.CacheHitRate < _thresholds.MinCacheHitRate)
                {
                    _logger.LogWarning($"Cache hit rate below threshold in {context}: {metrics.CacheHitRate}%");
                    shouldOptimize = true;
                }

                // Check active connections threshold
                if (metrics.ActiveConnections > _thresholds.MaxActiveConnections)
                {
                    _logger.LogWarning($"Active connections threshold exceeded in {context}: {metrics.ActiveConnections}");
                    shouldOptimize = true;
                }

                return shouldOptimize;
            }
            finally
            {
                _optimizationLock.Release();
            }
        }

        public async Task RecordMetricAsync(string context, TimeSpan responseTime, long memoryUsage)
        {
            var metrics = _metrics.GetOrAdd(context, _ => new PerformanceMetrics());

            await _optimizationLock.WaitAsync();
            try
            {
                // Update response time using exponential moving average
                var alpha = 0.3; // Smoothing factor
                metrics.AverageResponseTime = TimeSpan.FromTicks(
                    (long)(metrics.AverageResponseTime.Ticks * (1 - alpha) + responseTime.Ticks * alpha));

                // Update memory usage
                metrics.MemoryUsage = memoryUsage;

                // Update cache hit rate from monitor
                var cacheStats = await Task.Run(() => _monitor.GetCacheStatistics());
                metrics.CacheHitRate = (int)(cacheStats.Hits * 100.0 / (cacheStats.Hits + cacheStats.Misses));

                // Update active connections (simplified example)
                metrics.ActiveConnections = await Task.Run(() => _monitor.GetActiveConnections());

                metrics.LastUpdated = DateTime.UtcNow;

                // Log metrics for monitoring
                _logger.LogInformation(
                    $"Performance metrics for {context}: " +
                    $"Response Time={metrics.AverageResponseTime.TotalMilliseconds}ms, " +
                    $"Memory={metrics.MemoryUsage / (1024 * 1024)}MB, " +
                    $"Cache Hit Rate={metrics.CacheHitRate}%, " +
                    $"Connections={metrics.ActiveConnections}");
            }
            finally
            {
                _optimizationLock.Release();
            }
        }

        public async Task<PerformanceMetrics> GetMetricsAsync(string context)
        {
            return await Task.FromResult(_metrics.GetOrAdd(context, _ => new PerformanceMetrics()));
        }

        public void SetThresholds(PerformanceThresholds thresholds)
        {
            _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        }
    }
}
