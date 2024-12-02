using System;
using System.Threading.Tasks;

namespace GithubNote.NET.Services.UI
{
    public class LoadingIndicator : IActivityIndicator
    {
        public Task Show(string? message = default)
        {
            Console.WriteLine($"Showing loading indicator: {message}");
            return Task.CompletedTask;
        }

        public Task Hide()
        {
            Console.WriteLine("Hiding loading indicator");
            return Task.CompletedTask;
        }

        public bool IsVisible { get; private set; }
    }
}
