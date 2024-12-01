using GithubNote.NET.UI.Pages;

namespace GithubNote.NET.UI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(Routes.NoteEditor, typeof(NoteEditorPage));
        }
    }
}
