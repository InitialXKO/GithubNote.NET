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

    public class ThemeModeWrapper
    {
        public ThemeMode Mode { get; set; }

        public ThemeModeWrapper(ThemeMode mode)
        {
            Mode = mode;
        }
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
            _lightThemeOptions = new ThemeOptions();
            _darkThemeOptions = new ThemeOptions
            {
                BackgroundColor = "#1e1e1e",
                TextColor = "#ffffff"
            };
        }

        public async Task<ThemeMode> GetCurrentThemeAsync()
        {
            return _currentTheme;
        }

        public async Task SetThemeAsync(ThemeMode mode)
        {
            _currentTheme = mode;
            await _stateManager.SetStateAsync(ThemeModeKey, new ThemeModeWrapper(mode));
            ThemeChanged?.Invoke(this, mode);
        }

        public async Task<ThemeOptions> GetThemeOptionsAsync()
        {
            return _currentTheme == ThemeMode.Dark ? _darkThemeOptions : _lightThemeOptions;
        }

        public async Task SetThemeOptionsAsync(ThemeOptions options)
        {
            if (_currentTheme == ThemeMode.Dark)
                _darkThemeOptions = options;
            else
                _lightThemeOptions = options;
            
            await _stateManager.SetStateAsync(ThemeOptionsKey, options);
        }

        public string GetThemeClass()
        {
            return _currentTheme.ToString().ToLower();
        }

        public bool IsDarkMode()
        {
            return _currentTheme == ThemeMode.Dark;
        }

        public async Task<bool> ToggleDarkModeAsync()
        {
            await SetThemeAsync(_currentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark);
            return _currentTheme == ThemeMode.Dark;
        }

        public async Task ApplyThemeToElementAsync(object element, string propertyName)
        {
            var options = await GetThemeOptionsAsync();
            // Implementation depends on UI framework
        }

        public void LoadCustomTheme(string themePath)
        {
            try
            {
                // Load custom theme from file
                _logger.LogInformation($"Loading custom theme from {themePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load custom theme from {themePath}");
                throw;
            }
        }
    }
}
