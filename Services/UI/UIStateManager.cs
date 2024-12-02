using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GithubNote.NET.Services.Performance.Interfaces;

namespace GithubNote.NET.Services.UI
{
    public interface IUIStateManager
    {
        Task<T> GetStateAsync<T>(string key) where T : class;
        Task SetStateAsync<T>(string key, T state) where T : class;
        Task ClearStateAsync(string key);
        Task<bool> HasStateAsync(string key);
    }

    public class UIStateManager : IUIStateManager
    {
        private readonly ConcurrentDictionary<string, object> _states;
        private readonly ILogger<UIStateManager> _logger;
        private readonly IPerformanceMonitor _performanceMonitor;

        public UIStateManager(
            ILogger<UIStateManager> logger,
            IPerformanceMonitor performanceMonitor)
        {
            _states = new ConcurrentDictionary<string, object>();
            _logger = logger;
            _performanceMonitor = performanceMonitor;
        }

        public async Task<T> GetStateAsync<T>(string key) where T : class
        {
            var startTime = DateTime.UtcNow;
            try
            {
                if (_states.TryGetValue(key, out var state) && state is T typedState)
                {
                    return typedState;
                }
                return null;
            }
            finally
            {
                await _performanceMonitor.TrackOperationAsync(
                    $"UIStateManager.GetState<{typeof(T).Name}>",
                    DateTime.UtcNow - startTime);
            }
        }

        public async Task SetStateAsync<T>(string key, T state) where T : class
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _states.AddOrUpdate(key, state, (_, _) => state);
                _logger.LogDebug($"Updated UI state for key: {key}");
            }
            finally
            {
                await _performanceMonitor.TrackOperationAsync(
                    $"UIStateManager.SetState<{typeof(T).Name}>",
                    DateTime.UtcNow - startTime);
            }
        }

        public async Task ClearStateAsync(string key)
        {
            _states.TryRemove(key, out _);
            _logger.LogDebug($"Cleared UI state for key: {key}");
        }

        public async Task<bool> HasStateAsync(string key)
        {
            return _states.ContainsKey(key);
        }
    }
}
