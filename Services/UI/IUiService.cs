using System;
using System.Threading.Tasks;
using GithubNote.NET.Models;

namespace GithubNote.NET.Services.UI
{
    public interface IUiService
    {
        Task ShowErrorMessageAsync(string message, Exception ex = null);
        Task ShowSuccessMessageAsync(string message);
        Task<bool> ShowConfirmationDialogAsync(string message, string title = null);
        Task ShowLoadingIndicatorAsync(bool show);
        Task NavigateToNoteEditorAsync(Note note);
        Task NavigateToNoteListAsync();
        Task RefreshCurrentViewAsync();
        Task UpdateThemeAsync(string themeName);
    }
}
