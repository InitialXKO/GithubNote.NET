using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GithubNote.NET.Services.UI.Theme
{
    public enum ThemeMode
    {
        Light,
        Dark,
        System
    }

    public class ThemeOptions
    {
        public string PrimaryColor { get; set; } = "#2196f3";
        public string SecondaryColor { get; set; } = "#ff4081";
        public string BackgroundColor { get; set; } = "#ffffff";
        public string TextColor { get; set; } = "#000000";
        public string FontFamily { get; set; } = "Segoe UI, system-ui, sans-serif";
        public string CodeFontFamily { get; set; } = "Cascadia Code, Consolas, monospace";
        public int BorderRadius { get; set; } = 4;
        public double Spacing { get; set; } = 8;
    }

    public interface IThemeService
    {
        event EventHandler<ThemeMode> ThemeChanged;
        Task<ThemeMode> GetCurrentThemeAsync();
        Task SetThemeAsync(ThemeMode mode);
        Task<ThemeOptions> GetThemeOptionsAsync();
        Task SetThemeOptionsAsync(ThemeOptions options);
        string GetThemeClass();
    }

    public class ThemeService : IThemeService
    {
        private readonly ILogger<ThemeService> _logger;
        private readonly IUIStateManager _stateManager;
        private ThemeMode _currentTheme = ThemeMode.System;
        private ThemeOptions _lightThemeOptions;
        private ThemeOptions _darkThemeOptions;

        private const string ThemeModeKey = "theme_mode";
        private const string ThemeOptionsKey = "theme_options";

        public event EventHandler<ThemeMode> ThemeChanged;

        public ThemeService(
            ILogger<ThemeService> logger,
            IUIStateManager stateManager)
        {
            _logger = logger;
            _stateManager = stateManager;
            InitializeThemeOptions();
        }

        private void InitializeThemeOptions()
        {
            _lightThemeOptions = new ThemeOptions
            {
                PrimaryColor = "#2196f3",
                SecondaryColor = "#ff4081",
                BackgroundColor = "#ffffff",
                TextColor = "#000000"
            };

            _darkThemeOptions = new ThemeOptions
            {
                PrimaryColor = "#90caf9",
                SecondaryColor = "#f48fb1",
                BackgroundColor = "#121212",
                TextColor = "#ffffff"
            };
        }

        public async Task<ThemeMode> GetCurrentThemeAsync()
        {
            var savedTheme = await _stateManager.GetStateAsync<ThemeMode>(ThemeModeKey);
            _currentTheme = savedTheme ?? ThemeMode.System;
            return _currentTheme;
        }

        public async Task SetThemeAsync(ThemeMode mode)
        {
            _currentTheme = mode;
            await _stateManager.SetStateAsync(ThemeModeKey, mode);
            ThemeChanged?.Invoke(this, mode);
            _logger.LogInformation($"Theme changed to: {mode}");
        }

        public async Task<ThemeOptions> GetThemeOptionsAsync()
        {
            var currentTheme = await GetCurrentThemeAsync();
            var savedOptions = await _stateManager.GetStateAsync<ThemeOptions>(ThemeOptionsKey);

            if (savedOptions != null)
            {
                return savedOptions;
            }

            return currentTheme == ThemeMode.Dark ? _darkThemeOptions : _lightThemeOptions;
        }

        public async Task SetThemeOptionsAsync(ThemeOptions options)
        {
            await _stateManager.SetStateAsync(ThemeOptionsKey, options);
            _logger.LogInformation("Theme options updated");
        }

        public string GetThemeClass()
        {
            return _currentTheme switch
            {
                ThemeMode.Light => "theme-light",
                ThemeMode.Dark => "theme-dark",
                _ => "theme-system"
            };
        }
    }
}
