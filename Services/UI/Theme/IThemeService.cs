using System;
using System.Threading.Tasks;

namespace GithubNote.NET.Services.UI.Theme
{
    public interface IThemeService
    {
        event EventHandler<ThemeMode> ThemeChanged;
        Task<ThemeMode> GetCurrentThemeAsync();
        Task SetThemeAsync(ThemeMode mode);
        Task<ThemeOptions> GetThemeOptionsAsync();
        Task SetThemeOptionsAsync(ThemeOptions options);
        string GetThemeClass();
        Task<bool> ToggleDarkModeAsync();
        Task ApplyThemeToElementAsync(object element, string propertyName);
        bool IsDarkMode();
        void LoadCustomTheme(string themePath);
    }
}
