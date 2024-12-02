using System.Threading.Tasks;

namespace GithubNote.NET.Services.UI
{
    public interface IActivityIndicator
    {
        Task Show(string? message = default);
        Task Hide();
        bool IsVisible { get; }
    }
}
