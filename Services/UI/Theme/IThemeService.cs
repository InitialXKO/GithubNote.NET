using System.Threading.Tasks;

namespace GithubNote.NET.Services.UI.Theme
{
    public interface IThemeService
    {
        Task SetThemeAsync(string themeName);
        Task<string> GetCurrentThemeAsync();
        Task<bool> IsDarkModeAsync();
        Task ToggleDarkModeAsync();
        Task ApplyThemeToElementAsync(object element, string themeName = null);
        Task LoadCustomThemeAsync(string themePath);
    }
}
