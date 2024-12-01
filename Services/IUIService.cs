using System.Threading.Tasks;

namespace GithubNote.NET.Services
{
    public interface IUIService
    {
        Task ShowAlertAsync(string title, string message, string button = "OK");
        Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No");
        Task ShowLoadingAsync(string message = "Loading...");
        Task HideLoadingAsync();
        Task ShowToastAsync(string message, int durationMs = 3000);
        Task ShowErrorAsync(string message, string title = "Error");
    }
}
