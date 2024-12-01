using Microsoft.Maui.Controls;
using GithubNote.NET.UI.ViewModels;

namespace GithubNote.NET.UI.Pages
{
    public partial class NoteEditorPage : ContentPage
    {
        private readonly NoteEditorViewModel _viewModel;

        public NoteEditorPage(NoteEditorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        public async Task LoadNoteAsync(int noteId)
        {
            await _viewModel.LoadNoteAsync(noteId);
        }
    }
}
