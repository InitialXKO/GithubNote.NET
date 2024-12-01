using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace GithubNote.NET.Services
{
    public class UIService : IUIService
    {
        private IDispatcher _dispatcher => Application.Current.Dispatcher;
        private Page CurrentPage => Application.Current.MainPage;
        private IActivityIndicator _loadingIndicator;

        public UIService(IActivityIndicator loadingIndicator)
        {
            _loadingIndicator = loadingIndicator;
        }

        public async Task ShowAlertAsync(string title, string message, string button = "OK")
        {
            await _dispatcher.DispatchAsync(async () =>
            {
                await CurrentPage.DisplayAlert(title, message, button);
            });
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
        {
            return await _dispatcher.DispatchAsync(async () =>
            {
                return await CurrentPage.DisplayAlert(title, message, accept, cancel);
            });
        }

        public async Task ShowLoadingAsync(string message = "Loading...")
        {
            await _dispatcher.DispatchAsync(() =>
            {
                _loadingIndicator.Show(message);
                return Task.CompletedTask;
            });
        }

        public async Task HideLoadingAsync()
        {
            await _dispatcher.DispatchAsync(() =>
            {
                _loadingIndicator.Hide();
                return Task.CompletedTask;
            });
        }

        public async Task ShowToastAsync(string message, int durationMs = 3000)
        {
            await _dispatcher.DispatchAsync(async () =>
            {
                var snackbar = Application.Current.MainPage.FindByName<ISnackbar>("MainSnackbar");
                if (snackbar != null)
                {
                    await snackbar.ShowAsync(message, durationMs);
                }
            });
        }

        public async Task ShowErrorAsync(string message, string title = "Error")
        {
            await ShowAlertAsync(title, message);
        }
    }
}
