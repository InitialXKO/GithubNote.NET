namespace GithubNote.NET.UI.Controls
{
    public interface IActivityIndicator
    {
        void Show(string message = null);
        void Hide();
        bool IsVisible { get; }
    }
}
