using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GithubNote.NET.Services.Performance.Interfaces;
using GithubNote.NET.Services.Performance.Models;

namespace GithubNote.NET.Services.Performance
{
    public class PerformanceMonitor : GithubNote.NET.Services.Performance.Interfaces.IPerformanceMonitor
    {
        private readonly ConcurrentDictionary<string, List<double>> _operationDurations = new();
        private readonly ConcurrentDictionary<string, long> _memoryUsage = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastOperationTime = new();
        private readonly ConcurrentDictionary<string, double> _customMetrics = new();
        private readonly ILogger<PerformanceMonitor> _logger;
        private long _cacheHits;
        private long _cacheMisses;
        private int _activeConnections;

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, double>> GetAverageOperationDurations()
        {
            var averages = new Dictionary<string, double>();
            foreach (var kvp in _operationDurations)
            {
                if (kvp.Value.Any())
                {
                    // Calculate exponential moving average for more recent bias
                    var weights = Enumerable.Range(1, kvp.Value.Count)
                        .Select(i => Math.Exp(0.1 * i))
                        .ToList();
                    var weightedSum = kvp.Value.Zip(weights, (v, w) => v * w).Sum();
                    var weightSum = weights.Sum();
                    averages[kvp.Key] = weightedSum / weightSum;
                }
            }
            return averages;
        }

        public async Task<Dictionary<string, long>> GetMemoryUsageByContext()
        {
            return await Task.FromResult(_memoryUsage.ToDictionary(k => k.Key, v => v.Value));
        }

        public void TrackOperation(string operationName, TimeSpan duration)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentNullException(nameof(operationName));

            LogPerformanceMetric(operationName, duration.TotalMilliseconds);
        }

        public async Task TrackOperationAsync(string context, TimeSpan responseTime)
        {
            if (string.IsNullOrEmpty(context))
                throw new ArgumentNullException(nameof(context));

            await Task.Run(() => LogPerformanceMetric(context, responseTime.TotalMilliseconds));
        }

        public void LogPerformanceMetric(string metricName, double value)
        {
            if (string.IsNullOrEmpty(metricName))
                throw new ArgumentNullException(nameof(metricName));

            _operationDurations.AddOrUpdate(
                metricName,
                new List<double> { value },
                (_, list) =>
                {
                    list.Add(value);
                    if (list.Count > 100) // Keep only last 100 records
                    {
                        list.RemoveAt(0);
                    }
                    return list;
                });

            _customMetrics.AddOrUpdate(metricName, value, (_, __) => value);
            _lastOperationTime.AddOrUpdate(metricName, DateTime.UtcNow, (_, __) => DateTime.UtcNow);
            _logger.LogDebug($"Performance metric logged for {metricName}: {value}");
        }

        public async Task<PerformanceReport> GeneratePerformanceReport()
        {
            var durations = await GetAverageOperationDurations();
            var memoryUsage = await GetMemoryUsageByContext();
            
            return new PerformanceReport
            {
                AverageOperationDurations = durations,
                MemoryUsageByContext = memoryUsage,
                CustomMetrics = new Dictionary<string, double>(_customMetrics),
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task TrackMemoryUsageAsync(string context, long memoryUsage)
        {
            if (string.IsNullOrEmpty(context))
                throw new ArgumentNullException(nameof(context));

            _memoryUsage.AddOrUpdate(context, memoryUsage, (_, __) => memoryUsage);
            _logger.LogDebug($"Memory usage tracked for {context}: {memoryUsage / 1024 / 1024}MB");
        }

        public void TrackMemoryUsage(string context, long memoryUsageBytes)
        {
            if (string.IsNullOrEmpty(context))
                throw new ArgumentNullException(nameof(context));

            _memoryUsage.AddOrUpdate(context, memoryUsageBytes, (_, __) => memoryUsageBytes);
            _logger.LogDebug($"Memory usage tracked for {context}: {memoryUsageBytes} bytes");
        }

        public async Task<PerformanceMetrics> GetMetricsAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new PerformanceMetrics
                {
                    AverageOperationDurations = GetAverageOperationDurations().Result,
                    MemoryUsageByContext = GetMemoryUsageByContext().Result,
                    TotalMemoryUsed = _memoryUsage.Values.Sum(),
                    CacheHitCount = _cacheHits,
                    CacheMissCount = _cacheMisses
                };

                return metrics;
            });
        }

        public async Task<int> GetActiveConnectionsAsync()
        {
            return _activeConnections;
        }

        public async Task<CacheStatistics> GetCacheStatisticsAsync()
        {
            return new CacheStatistics
            {
                TotalCacheSize = _memoryUsage.Values.Sum(),
                CacheHitRate = _cacheHits / (double)(_cacheHits + _cacheMisses),
                CacheEntries = _memoryUsage.Count,
                LastUpdated = DateTime.UtcNow
            };
        }

        public void IncrementActiveConnections()
        {
            Interlocked.Increment(ref _activeConnections);
        }

        public void DecrementActiveConnections()
        {
            Interlocked.Decrement(ref _activeConnections);
        }

        public void RecordCacheHit()
        {
            Interlocked.Increment(ref _cacheHits);
        }

        public void RecordCacheMiss()
        {
            Interlocked.Increment(ref _cacheMisses);
        }

        public void ResetMetrics()
        {
            _operationDurations.Clear();
            _memoryUsage.Clear();
            _cacheHits = 0;
            _cacheMisses = 0;
            _activeConnections = 0;
        }

        internal void TrackCacheHit() => RecordCacheHit();
        internal void TrackCacheMiss() => RecordCacheMiss();

        private double CalculateCacheHitRate()
        {
            var total = _cacheHits + _cacheMisses;
            if (total == 0)
                return 0;

            return (_cacheHits * 100.0) / total;
        }

        public async Task<PerformanceReport> GeneratePerformanceReportAsync()
        {
            var report = new PerformanceReport
            {
                AverageOperationDurations = await GetAverageOperationDurations(),
                MemoryUsageByContext = await GetMemoryUsageByContext(),
                CacheHitRate = CalculateCacheHitRate()
            };

            return report;
        }

        public async Task<PerformanceMetrics> GetMetrics()
        {
            return new PerformanceMetrics
            {
                AverageOperationDurations = await GetAverageOperationDurations(),
                MemoryUsageByContext = await GetMemoryUsageByContext(),
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}
