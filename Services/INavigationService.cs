using System;
using System.Threading.Tasks;

namespace GithubNote.NET.Services
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route);
        Task NavigateToAsync(string route, object parameter);
        Task GoBackAsync();
        Task GoToRootAsync();
        string GetCurrentRoute();
        void RegisterRoute(string route, Type pageType);
    }
}
