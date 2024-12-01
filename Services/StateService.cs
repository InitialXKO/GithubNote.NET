using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace GithubNote.NET.Services
{
    public class StateService : IStateService
    {
        private readonly IPreferences _preferences;
        private const string StatePrefix = "app_state_";

        public StateService(IPreferences preferences)
        {
            _preferences = preferences;
        }

        public async Task SaveStateAsync<T>(string key, T state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state);
                await Task.Run(() => _preferences.Set(GetPrefKey(key), json));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save state for key {key}", ex);
            }
        }

        public async Task<T> LoadStateAsync<T>(string key)
        {
            try
            {
                var json = await Task.Run(() => _preferences.Get(GetPrefKey(key), string.Empty));
                if (string.IsNullOrEmpty(json))
                {
                    return default;
                }
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load state for key {key}", ex);
            }
        }

        public async Task ClearStateAsync(string key)
        {
            await Task.Run(() => _preferences.Remove(GetPrefKey(key)));
        }

        public async Task<bool> HasStateAsync(string key)
        {
            return await Task.Run(() => _preferences.ContainsKey(GetPrefKey(key)));
        }

        private string GetPrefKey(string key) => $"{StatePrefix}{key}";
    }
}
