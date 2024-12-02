using Microsoft.Maui.Controls;

namespace GithubNote.NET.UI.Controls
{
    public partial class LoadingIndicator : ContentView, IActivityIndicator
    {
        private bool _isVisible;
        private string _message = string.Empty;

        public LoadingIndicator()
        {
            InitializeComponent();
            BindingContext = this;
        }

        public new bool IsVisible
        {
            get => _isVisible;
            private set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Message
        {
            get => _message;
            private set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged();
                }
            }
        }

        public void Show(string? message = "")
        {
            Message = message ?? "Loading...";
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
            Message = string.Empty;
        }
    }
}
