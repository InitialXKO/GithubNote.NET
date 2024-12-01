using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GithubNote.NET.Models;
using GithubNote.NET.Services.Performance;
using Microsoft.Extensions.Logging;

namespace GithubNote.NET.Cache
{
    public class OptimizedNoteCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry<Note>> _noteCache;
        private readonly ConcurrentDictionary<string, CacheEntry<List<Note>>> _listCache;
        private readonly IPerformanceOptimizer _optimizer;
        private readonly ILogger<OptimizedNoteCache> _logger;
        private readonly SemaphoreSlim _cleanupLock;
        private readonly Timer _cleanupTimer;
        private const int CleanupIntervalMinutes = 5;

        private class CacheEntry<T>
        {
            public T Value { get; set; }
            public DateTime LastAccessed { get; set; }
            public int AccessCount { get; set; }
            public long Size { get; set; }
        }

        public OptimizedNoteCache(
            IPerformanceOptimizer optimizer,
            ILogger<OptimizedNoteCache> logger)
        {
            _noteCache = new ConcurrentDictionary<string, CacheEntry<Note>>();
            _listCache = new ConcurrentDictionary<string, CacheEntry<List<Note>>>();
            _optimizer = optimizer;
            _logger = logger;
            _cleanupLock = new SemaphoreSlim(1, 1);

            // Start cleanup timer
            _cleanupTimer = new Timer(
                CleanupCache,
                null,
                TimeSpan.FromMinutes(CleanupIntervalMinutes),
                TimeSpan.FromMinutes(CleanupIntervalMinutes));
        }

        public async Task<Note> GetNoteAsync(string id)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                if (_noteCache.TryGetValue(id, out var entry))
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    Interlocked.Increment(ref entry.AccessCount);
                    return entry.Value;
                }
                return null;
            }
            finally
            {
                await _optimizer.RecordMetricAsync(
                    "NoteCache",
                    DateTime.UtcNow - startTime,
                    GetTotalCacheSize());
            }
        }

        public async Task<List<Note>> GetNotesAsync()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var key = "all_notes";
                if (_listCache.TryGetValue(key, out var entry))
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    Interlocked.Increment(ref entry.AccessCount);
                    return entry.Value;
                }
                return null;
            }
            finally
            {
                await _optimizer.RecordMetricAsync(
                    "NoteCache",
                    DateTime.UtcNow - startTime,
                    GetTotalCacheSize());
            }
        }

        public async Task SetNoteAsync(Note note)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var entry = new CacheEntry<Note>
                {
                    Value = note,
                    LastAccessed = DateTime.UtcNow,
                    AccessCount = 0,
                    Size = EstimateNoteSize(note)
                };

                _noteCache.AddOrUpdate(note.Id, entry, (_, _) => entry);

                // Invalidate list cache when a note is updated
                _listCache.Clear();

                if (await _optimizer.ShouldOptimizeAsync("NoteCache"))
                {
                    await OptimizeCacheAsync();
                }
            }
            finally
            {
                await _optimizer.RecordMetricAsync(
                    "NoteCache",
                    DateTime.UtcNow - startTime,
                    GetTotalCacheSize());
            }
        }

        public async Task SetNotesAsync(List<Note> notes)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var entry = new CacheEntry<List<Note>>
                {
                    Value = notes,
                    LastAccessed = DateTime.UtcNow,
                    AccessCount = 0,
                    Size = notes.Sum(n => EstimateNoteSize(n))
                };

                _listCache.AddOrUpdate("all_notes", entry, (_, _) => entry);

                if (await _optimizer.ShouldOptimizeAsync("NoteCache"))
                {
                    await OptimizeCacheAsync();
                }
            }
            finally
            {
                await _optimizer.RecordMetricAsync(
                    "NoteCache",
                    DateTime.UtcNow - startTime,
                    GetTotalCacheSize());
            }
        }

        public async Task InvalidateNoteAsync(string id)
        {
            _noteCache.TryRemove(id, out _);
            _listCache.Clear();
        }

        private async void CleanupCache(object state)
        {
            if (await _cleanupLock.WaitAsync(0)) // Non-blocking
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var expirationTime = TimeSpan.FromHours(1);

                    // Remove expired note entries
                    var expiredNotes = _noteCache
                        .Where(kvp => now - kvp.Value.LastAccessed > expirationTime)
                        .ToList();

                    foreach (var expired in expiredNotes)
                    {
                        _noteCache.TryRemove(expired.Key, out _);
                    }

                    // Remove expired list entries
                    var expiredLists = _listCache
                        .Where(kvp => now - kvp.Value.LastAccessed > expirationTime)
                        .ToList();

                    foreach (var expired in expiredLists)
                    {
                        _listCache.TryRemove(expired.Key, out _);
                    }

                    if (expiredNotes.Any() || expiredLists.Any())
                    {
                        _logger.LogInformation(
                            $"Cache cleanup: Removed {expiredNotes.Count} notes and {expiredLists.Count} lists");
                    }
                }
                finally
                {
                    _cleanupLock.Release();
                }
            }
        }

        private async Task OptimizeCacheAsync()
        {
            if (await _cleanupLock.WaitAsync(0)) // Non-blocking
            {
                try
                {
                    var metrics = await _optimizer.GetMetricsAsync("NoteCache");
                    var totalSize = GetTotalCacheSize();

                    // If memory usage is high, remove least accessed items
                    if (totalSize > 100 * 1024 * 1024) // 100MB
                    {
                        var leastAccessedNotes = _noteCache
                            .OrderBy(kvp => kvp.Value.AccessCount)
                            .Take(_noteCache.Count / 4) // Remove 25% of items
                            .ToList();

                        foreach (var item in leastAccessedNotes)
                        {
                            _noteCache.TryRemove(item.Key, out _);
                        }

                        _logger.LogInformation(
                            $"Cache optimized: Removed {leastAccessedNotes.Count} least accessed notes");
                    }
                }
                finally
                {
                    _cleanupLock.Release();
                }
            }
        }

        private long GetTotalCacheSize()
        {
            return _noteCache.Values.Sum(e => e.Size) + 
                   _listCache.Values.Sum(e => e.Size);
        }

        private long EstimateNoteSize(Note note)
        {
            // Rough estimate of note size in bytes
            return (note.Id?.Length ?? 0) * 2 +
                   (note.Title?.Length ?? 0) * 2 +
                   (note.Content?.Length ?? 0) * 2 +
                   100; // Additional overhead for other properties
        }
    }
}
