using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GithubNote.NET.Models;
using GithubNote.NET.Services;
using GithubNote.NET.Services.UI;
using GithubNote.NET.UI.ViewModels.States;

namespace GithubNote.NET.UI.ViewModels
{
    public class NoteEditorViewModel : BaseViewModel, IQueryAttributable
    {
        private readonly INoteService _noteService;
        private readonly INoteSync _noteSync;
        private readonly INavigationService _navigationService;
        private readonly IStateService _stateService;
        private readonly IUiService _uiService;
        private const string StateKey = "NoteEditorState";
        private Note _currentNote;
        private string _content;
        private bool _isEditing;
        private bool _isSynced;

        public NoteEditorViewModel(
            INoteService noteService,
            INoteSync noteSync,
            INavigationService navigationService,
            IStateService stateService,
            IUiService uiService)
        {
            _noteService = noteService;
            _noteSync = noteSync;
            _navigationService = navigationService;
            _stateService = stateService;
            _uiService = uiService;

            // 命令初始化
            SaveCommand = new AsyncRelayCommand(SaveNoteAsync, CanSave);
            SyncCommand = new AsyncRelayCommand(SyncNoteAsync);
            AddCategoryCommand = new AsyncRelayCommand<string>(AddCategoryAsync);
            RemoveCategoryCommand = new AsyncRelayCommand<string>(RemoveCategoryAsync);
            AddCommentCommand = new AsyncRelayCommand<string>(AddCommentAsync);
            AddAttachmentCommand = new AsyncRelayCommand<ImageAttachment>(AddAttachmentAsync);

            Categories = new ObservableCollection<string>();
            Comments = new ObservableCollection<Comment>();
            Attachments = new ObservableCollection<ImageAttachment>();
        }

        public Note CurrentNote
        {
            get => _currentNote;
            set
            {
                if (SetProperty(ref _currentNote, value))
                {
                    Title = value?.Title ?? "New Note";
                    Content = value?.Content;
                    LoadNoteDetails();
                }
            }
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool IsSynced
        {
            get => _isSynced;
            set => SetProperty(ref _isSynced, value);
        }

        public ObservableCollection<string> Categories { get; }
        public ObservableCollection<Comment> Comments { get; }
        public ObservableCollection<ImageAttachment> Attachments { get; }

        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand SyncCommand { get; }
        public IAsyncRelayCommand<string> AddCategoryCommand { get; }
        public IAsyncRelayCommand<string> RemoveCategoryCommand { get; }
        public IAsyncRelayCommand<string> AddCommentCommand { get; }
        public IAsyncRelayCommand<ImageAttachment> AddAttachmentCommand { get; }

        public async Task LoadNoteAsync(string noteId)
        {
            try
            {
                IsBusy = true;
                CurrentNote = await _noteService.GetNoteAsync(noteId);
                Content = CurrentNote.Content;
                Categories.Clear();
                foreach (var category in CurrentNote.Categories)
                {
                    Categories.Add(category);
                }
                Comments.Clear();
                foreach (var comment in CurrentNote.Comments)
                {
                    Comments.Add(comment);
                }
                Attachments.Clear();
                foreach (var attachment in CurrentNote.Attachments)
                {
                    if (attachment is ImageAttachment imageAttachment)
                    {
                        Attachments.Add(imageAttachment);
                    }
                }
                IsSynced = CurrentNote.IsSynced;
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorMessageAsync("Failed to load note", new Exception(ex.Message));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("noteId", out var noteId))
            {
                if (!string.IsNullOrEmpty(noteId?.ToString()))
                {
                    _ = LoadNoteAsync(noteId.ToString());
                }
            }
        }

        private async Task RestoreStateAsync(string noteId)
        {
            try
            {
                var state = await _stateService.LoadStateAsync<NoteEditorState>($"{StateKey}_{noteId}");
                if (state != null)
                {
                    Title = state.Title;
                    Content = state.Content;
                    if (state.Categories?.Count > 0)
                    {
                        Categories.Clear();
                        foreach (var category in state.Categories)
                        {
                            Categories.Add(category);
                        }
                    }
                    if (!string.IsNullOrEmpty(state.DraftContent))
                    {
                        await _uiService.ShowConfirmationDialogAsync(
                            "Restore Draft",
                            "Would you like to restore your unsaved changes?");
                        if (true) // User confirmed
                        {
                            Content = state.DraftContent;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorMessageAsync($"Failed to restore state: {ex.Message}", new Exception(ex.Message));
            }
        }

        private async Task SaveStateAsync()
        {
            try
            {
                var state = new NoteEditorState
                {
                    NoteId = CurrentNote?.Id,
                    Title = Title,
                    Content = Content,
                    Categories = Categories?.ToList(),
                    IsDirty = IsEditing,
                    DraftContent = IsEditing ? Content : null
                };
                await _stateService.SaveStateAsync($"{StateKey}_{CurrentNote?.Id}", state);
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorMessageAsync($"Failed to save state: {ex.Message}", new Exception(ex.Message));
            }
        }

        private async Task SaveNoteAsync()
        {
            try
            {
                await _uiService.ShowLoadingIndicatorAsync(true);
                CurrentNote.Title = Title;
                CurrentNote.Content = Content;
                CurrentNote.Categories = Categories.ToList();
                CurrentNote.UpdatedAt = DateTime.UtcNow;

                await _noteService.SaveNoteAsync(CurrentNote);
                await SaveStateAsync();
                await _stateService.ClearStateAsync($"{StateKey}_{CurrentNote.Id}");
                await _navigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorMessageAsync($"Failed to save note: {ex.Message}", new Exception(ex.Message));
            }
            finally
            {
                await _uiService.ShowLoadingIndicatorAsync(false);
            }
        }

        private bool CanSave()
        {
            return IsEditing && CurrentNote != null && !string.IsNullOrWhiteSpace(Content);
        }

        private async Task SyncNoteAsync()
        {
            try
            {
                await _uiService.ShowLoadingIndicatorAsync(true);
                SetBusy(true);
                if (CurrentNote != null)
                {
                    await _noteService.SyncWithGistAsync(CurrentNote.Id);
                    var syncStatus = await _noteSync.GetSyncStatusAsync(CurrentNote.Id);
                    IsSynced = syncStatus.IsSynced;
                }
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorMessageAsync($"Error syncing note: {ex.Message}", new Exception(ex.Message));
            }
            finally
            {
                await _uiService.ShowLoadingIndicatorAsync(false);
                SetBusy(false);
            }
        }

        private async Task AddCategoryAsync(string category)
        {
            try
            {
                await _uiService.ShowLoadingIndicatorAsync(true);
                SetBusy(true);
                if (CurrentNote != null && !string.IsNullOrWhiteSpace(category))
                {
                    await _noteService.AddCategoryAsync(CurrentNote.Id, category);
                    if (!Categories.Contains(category))
                    {
                        Categories.Add(category);
                    }
                }
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorMessageAsync($"Error adding category: {ex.Message}", new Exception(ex.Message));
            }
            finally
            {
                await _uiService.ShowLoadingIndicatorAsync(false);
                SetBusy(false);
            }
        }

        private async Task RemoveCategoryAsync(string category)
        {
            try
            {
                await _uiService.ShowLoadingIndicatorAsync(true);
                SetBusy(true);
                if (CurrentNote != null && !string.IsNullOrWhiteSpace(category))
                {
                    await _noteService.RemoveCategoryAsync(CurrentNote.Id, category);
                    Categories.Remove(category);
                }
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorMessageAsync($"Error removing category: {ex.Message}", new Exception(ex.Message));
            }
            finally
            {
                await _uiService.ShowLoadingIndicatorAsync(false);
                SetBusy(false);
            }
        }

        private async Task AddCommentAsync(string content)
        {
            try
            {
                await _uiService.ShowLoadingIndicatorAsync(true);
                SetBusy(true);
                if (CurrentNote != null && !string.IsNullOrWhiteSpace(content))
                {
                    var comment = new Comment
                    {
                        Content = content,
                        CreatedAt = DateTime.UtcNow
                    };

                    var updatedNote = await _noteService.AddCommentAsync(CurrentNote.Id, comment);
                    CurrentNote = updatedNote;
                    Comments.Add(comment);
                }
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorMessageAsync($"Error adding comment: {ex.Message}", new Exception(ex.Message));
            }
            finally
            {
                await _uiService.ShowLoadingIndicatorAsync(false);
                SetBusy(false);
            }
        }

        private async Task AddAttachmentAsync(ImageAttachment attachment)
        {
            try
            {
                await _uiService.ShowLoadingIndicatorAsync(true);
                SetBusy(true);
                if (CurrentNote != null && attachment != null)
                {
                    var updatedNote = await _noteService.AddAttachmentAsync(CurrentNote.Id, attachment);
                    CurrentNote = updatedNote;
                    Attachments.Add(attachment);
                }
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorMessageAsync($"Error adding attachment: {ex.Message}", new Exception(ex.Message));
            }
            finally
            {
                await _uiService.ShowLoadingIndicatorAsync(false);
                SetBusy(false);
            }
        }

        private void LoadNoteDetails()
        {
            if (CurrentNote != null)
            {
                Categories.Clear();
                Comments.Clear();
                Attachments.Clear();

                if (CurrentNote.Categories != null)
                {
                    foreach (var category in CurrentNote.Categories)
                    {
                        Categories.Add(category);
                    }
                }

                if (CurrentNote.Comments != null)
                {
                    foreach (var comment in CurrentNote.Comments)
                    {
                        Comments.Add(comment);
                    }
                }

                if (CurrentNote.Attachments != null)
                {
                    foreach (var attachment in CurrentNote.Attachments)
                    {
                        if (attachment is ImageAttachment imageAttachment)
                        {
                            Attachments.Add(imageAttachment);
                        }
                    }
                }
            }
        }
    }
}
