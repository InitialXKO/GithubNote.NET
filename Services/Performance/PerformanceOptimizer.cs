using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using GithubNote.NET.Services.Performance.Models;
using GithubNote.NET.Services.Performance.Interfaces;

namespace GithubNote.NET.Services.Performance.Implementation
{
    public class PerformanceOptimizer : IPerformanceOptimizer
    {
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics;
        private readonly PerformanceThresholds _thresholds;
        private readonly ILogger<PerformanceOptimizer> _logger;
        private readonly IMemoryCache _cache;
        private readonly IPerformanceMonitor _performanceMonitor;

        public PerformanceOptimizer(
            ILogger<PerformanceOptimizer> logger, 
            IMemoryCache cache,
            IPerformanceMonitor performanceMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _metrics = new ConcurrentDictionary<string, PerformanceMetrics>();
            _thresholds = new PerformanceThresholds();
        }

        public async Task<bool> ShouldOptimizeAsync(string context)
        {
            if (string.IsNullOrEmpty(context))
                throw new ArgumentNullException(nameof(context));

            var metrics = await GetMetricsAsync(context);
            var performanceReport = await _performanceMonitor.GeneratePerformanceReport();
            var activeConnections = await _performanceMonitor.GetActiveConnectionsAsync();

            var shouldOptimize = false;
            var reasons = new List<string>();

            if (metrics.AverageResponseTime > _thresholds.MaxResponseTime)
            {
                reasons.Add($"Response time ({metrics.AverageResponseTime.TotalMilliseconds:F2}ms) exceeds threshold ({_thresholds.MaxResponseTime.TotalMilliseconds}ms)");
                shouldOptimize = true;
            }

            if (metrics.MemoryUsage > _thresholds.MaxMemoryUsage)
            {
                reasons.Add($"Memory usage ({metrics.MemoryUsage / 1024 / 1024}MB) exceeds threshold ({_thresholds.MaxMemoryUsage / 1024 / 1024}MB)");
                shouldOptimize = true;
            }

            if (activeConnections > _thresholds.MaxActiveConnections)
            {
                reasons.Add($"Active connections ({activeConnections}) exceeds threshold ({_thresholds.MaxActiveConnections})");
                shouldOptimize = true;
            }

            if (performanceReport.CacheHitRate < _thresholds.MinCacheHitRate)
            {
                reasons.Add($"Cache hit rate ({performanceReport.CacheHitRate:F2}%) below threshold ({_thresholds.MinCacheHitRate}%)");
                shouldOptimize = true;
            }

            if (shouldOptimize)
            {
                _logger.LogWarning($"Optimization needed for {context}. Reasons:\n- {string.Join("\n- ", reasons)}");
            }

            return shouldOptimize;
        }

        public async Task RecordMetricAsync(string context, TimeSpan responseTime, long memoryUsage)
        {
            if (string.IsNullOrEmpty(context))
                throw new ArgumentNullException(nameof(context));

            try
            {
                await _performanceMonitor.TrackOperationAsync(context, responseTime);
                await _performanceMonitor.TrackMemoryUsageAsync(context, memoryUsage);

                var metrics = _metrics.GetOrAdd(context, _ => new PerformanceMetrics());
                metrics.LastUpdated = DateTime.UtcNow;

                // Update metrics with exponential moving average
                const double alpha = 0.2; // Smoothing factor
                metrics.AverageResponseTime = new TimeSpan(
                    (long)(alpha * responseTime.Ticks + (1 - alpha) * metrics.AverageResponseTime.Ticks));
                metrics.MemoryUsage = (long)(alpha * memoryUsage + (1 - alpha) * metrics.MemoryUsage);

                _logger.LogDebug($"Updated performance metrics for context: {context}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recording metrics for context: {context}");
                throw;
            }
        }

        public async Task<PerformanceMetrics> GetMetricsAsync(string context)
        {
            if (string.IsNullOrEmpty(context))
                throw new ArgumentNullException(nameof(context));

            var metrics = _metrics.GetOrAdd(context, _ => new PerformanceMetrics
            {
                AverageResponseTime = TimeSpan.Zero,
                MemoryUsage = 0,
                LastUpdated = DateTime.UtcNow
            });

            var monitorMetrics = await _performanceMonitor.GetMetricsAsync();
            metrics.AverageOperationDurations = monitorMetrics.AverageOperationDurations;
            metrics.MemoryUsageByContext = monitorMetrics.MemoryUsageByContext;

            return metrics;
        }

        public void SetThresholds(PerformanceThresholds thresholds)
        {
            if (thresholds == null)
                throw new ArgumentNullException(nameof(thresholds));

            _thresholds.MaxResponseTime = thresholds.MaxResponseTime;
            _thresholds.MaxMemoryUsage = thresholds.MaxMemoryUsage;
            _thresholds.MaxActiveConnections = thresholds.MaxActiveConnections;
            _thresholds.MinCacheHitRate = thresholds.MinCacheHitRate;
        }
    }
}
