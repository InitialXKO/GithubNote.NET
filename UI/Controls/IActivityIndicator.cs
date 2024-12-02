namespace GithubNote.NET.UI.Controls
{
    public interface IActivityIndicator
    {
        void Show(string? message = "");
        void Hide();
        bool IsVisible { get; }
    }
}
