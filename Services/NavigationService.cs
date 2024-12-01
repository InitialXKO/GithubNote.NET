using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace GithubNote.NET.Services
{
    public class NavigationService : INavigationService
    {
        public Task NavigateToAsync(string route)
        {
            return Shell.Current.GoToAsync(route);
        }

        public Task NavigateToAsync(string route, object parameter)
        {
            return Shell.Current.GoToAsync(route, parameter);
        }

        public Task GoBackAsync()
        {
            return Shell.Current.GoToAsync("..");
        }

        public Task GoToRootAsync()
        {
            return Shell.Current.GoToAsync("//");
        }

        public string GetCurrentRoute()
        {
            return Shell.Current.CurrentState.Location.ToString();
        }

        public void RegisterRoute(string route, Type pageType)
        {
            Routing.RegisterRoute(route, pageType);
        }
    }
}
