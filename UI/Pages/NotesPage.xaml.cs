using Microsoft.Maui.Controls;
using GithubNote.NET.UI.ViewModels;

namespace GithubNote.NET.UI.Pages
{
    public partial class NotesPage : ContentPage
    {
        private readonly NoteListViewModel _viewModel;

        public NotesPage(NoteListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadNotesAsync();
        }
    }
}
