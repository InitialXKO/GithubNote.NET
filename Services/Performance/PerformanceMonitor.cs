using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GithubNote.NET.Services.Performance
{
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly ConcurrentDictionary<string, List<TimeSpan>> _operationDurations;
        private readonly ConcurrentDictionary<string, long> _memoryUsage;
        private int _cacheHits;
        private int _cacheMisses;

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger;
            _operationDurations = new ConcurrentDictionary<string, List<TimeSpan>>();
            _memoryUsage = new ConcurrentDictionary<string, long>();
        }

        public void TrackOperation(string operationName, TimeSpan duration)
        {
            _operationDurations.AddOrUpdate(
                operationName,
                new List<TimeSpan> { duration },
                (_, list) =>
                {
                    list.Add(duration);
                    return list;
                });

            if (duration > TimeSpan.FromSeconds(1))
            {
                _logger.LogWarning($"Slow operation detected: {operationName} took {duration.TotalMilliseconds}ms");
            }
        }

        public void TrackMemoryUsage(string context, long bytesUsed)
        {
            _memoryUsage.AddOrUpdate(
                context,
                bytesUsed,
                (_, current) => current + bytesUsed);

            if (bytesUsed > 1024 * 1024 * 10) // 10MB
            {
                _logger.LogWarning($"High memory usage detected in {context}: {bytesUsed / (1024 * 1024)}MB");
            }
        }

        public async Task<PerformanceMetrics> GetMetricsAsync()
        {
            return await Task.Run(() =>
            {
                var metrics = new PerformanceMetrics
                {
                    AverageOperationDurations = _operationDurations.ToDictionary(
                        kvp => kvp.Key,
                        kvp => TimeSpan.FromMilliseconds(kvp.Value.Average(d => d.TotalMilliseconds))),
                    MemoryUsageByContext = new Dictionary<string, long>(_memoryUsage),
                    TotalMemoryUsed = _memoryUsage.Values.Sum(),
                    CacheHitCount = _cacheHits,
                    CacheMissCount = _cacheMisses
                };

                return metrics;
            });
        }

        public void ResetMetrics()
        {
            _operationDurations.Clear();
            _memoryUsage.Clear();
            _cacheHits = 0;
            _cacheMisses = 0;
        }

        internal void TrackCacheHit() => Interlocked.Increment(ref _cacheHits);
        internal void TrackCacheMiss() => Interlocked.Increment(ref _cacheMisses);
    }
}
