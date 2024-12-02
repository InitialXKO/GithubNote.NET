using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using GithubNote.NET.Models;
using GithubNote.NET.Services;

namespace GithubNote.NET.UI.ViewModels
{
    public class NoteListViewModel : BaseViewModel
    {
        private readonly INoteService _noteService;
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigationService;
        private readonly IUIService _uiService;
        private readonly IStateService _stateService;
        private const string StateKey = "NoteListState";
        private ObservableCollection<Note> _notes;
        private string _searchQuery;
        private string _selectedCategory;

        public NoteListViewModel(
            INoteService noteService,
            IAuthenticationService authService,
            INavigationService navigationService,
            IUIService uiService,
            IStateService stateService)
        {
            _noteService = noteService;
            _authService = authService;
            _navigationService = navigationService;
            _uiService = uiService;
            _stateService = stateService;
            Notes = new ObservableCollection<Note>();

            // 命令初始化
            RefreshCommand = new AsyncRelayCommand(LoadNotesAsync);
            SearchCommand = new AsyncRelayCommand(SearchNotesAsync);
            CreateNoteCommand = new AsyncRelayCommand(CreateNoteAsync);
            DeleteNoteCommand = new AsyncRelayCommand<Note>(DeleteNoteAsync);
            FilterByCategoryCommand = new AsyncRelayCommand<string>(FilterByCategoryAsync);
            SyncCommand = new AsyncRelayCommand(SyncNotesAsync);

            Title = "Notes";
            
            // 恢复状态
            RestoreStateAsync().ConfigureAwait(false);
        }

        public ObservableCollection<Note> Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    SearchCommand.Execute(null);
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    FilterByCategoryCommand.Execute(value);
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public IAsyncRelayCommand SearchCommand { get; }
        public IAsyncRelayCommand CreateNoteCommand { get; }
        public IAsyncRelayCommand<Note> DeleteNoteCommand { get; }
        public IAsyncRelayCommand<string> FilterByCategoryCommand { get; }
        public IAsyncRelayCommand SyncCommand { get; }

        private async Task RestoreStateAsync()
        {
            try
            {
                var state = await _stateService.LoadStateAsync<NoteListState>(StateKey);
                if (state != null)
                {
                    if (state.Notes?.Count > 0)
                    {
                        Notes = new ObservableCollection<Note>(state.Notes);
                    }
                    SearchQuery = state.SearchQuery;
                    SelectedCategory = state.SelectedCategory;
                }
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorAsync($"Failed to restore state: {ex.Message}");
            }
        }

        private async Task SaveStateAsync()
        {
            try
            {
                var state = new NoteListState
                {
                    Notes = Notes?.ToList(),
                    SearchQuery = SearchQuery,
                    SelectedCategory = SelectedCategory,
                    IsSearchActive = !string.IsNullOrEmpty(SearchQuery),
                    LastSyncTime = DateTime.UtcNow.ToString("o")
                };
                await _stateService.SaveStateAsync(StateKey, state);
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorAsync($"Failed to save state: {ex.Message}");
            }
        }

        public async Task LoadNotesAsync()
        {
            try
            {
                await _uiService.ShowLoadingAsync("Loading notes...");
                var user = await _authService.GetCurrentUserAsync();
                if (user == null)
                {
                    await _uiService.ShowErrorAsync("User not authenticated");
                    return;
                }

                var notes = await _noteService.GetNotesByUserAsync(user.Id);
                Notes.Clear();
                foreach (var note in notes)
                {
                    Notes.Add(note);
                }
                await SaveStateAsync();
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorAsync($"Error loading notes: {ex.Message}");
            }
            finally
            {
                await _uiService.HideLoadingAsync();
            }
        }

        private async Task SearchNotesAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchQuery))
                {
                    await LoadNotesAsync();
                    return;
                }

                await _uiService.ShowLoadingAsync("Searching notes...");
                var user = await _authService.GetCurrentUserAsync();
                var notes = await _noteService.SearchNotesAsync(SearchQuery, user.Id);
                Notes.Clear();
                foreach (var note in notes)
                {
                    Notes.Add(note);
                }
                await SaveStateAsync();
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorAsync($"Error searching notes: {ex.Message}");
            }
            finally
            {
                await _uiService.HideLoadingAsync();
            }
        }

        private async Task CreateNoteAsync()
        {
            try
            {
                await _uiService.ShowLoadingAsync("Creating note...");
                var user = await _authService.GetCurrentUserAsync();
                var note = new Note
                {
                    Title = "New Note",
                    Content = string.Empty,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdNote = await _noteService.CreateNoteAsync(note);
                Notes.Add(createdNote);
                await SaveStateAsync();
                await _uiService.ShowToastAsync("Note created successfully");
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorAsync($"Error creating note: {ex.Message}");
            }
            finally
            {
                await _uiService.HideLoadingAsync();
            }
        }

        private async Task DeleteNoteAsync(Note note)
        {
            if (note == null) return;

            try
            {
                var confirmed = await _uiService.ShowConfirmationAsync(
                    "Delete Note",
                    $"Are you sure you want to delete '{note.Title}'?");

                if (!confirmed) return;

                await _uiService.ShowLoadingAsync("Deleting note...");
                await _noteService.DeleteNoteAsync(note.Id);
                Notes.Remove(note);
                await SaveStateAsync();
                await _uiService.ShowToastAsync("Note deleted successfully");
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorAsync($"Error deleting note: {ex.Message}");
            }
            finally
            {
                await _uiService.HideLoadingAsync();
            }
        }

        private async Task FilterByCategoryAsync(string category)
        {
            try
            {
                await _uiService.ShowLoadingAsync("Filtering notes...");
                var user = await _authService.GetCurrentUserAsync();
                var notes = await _noteService.GetNotesByCategoryAsync(category, user.Id);
                Notes.Clear();
                foreach (var note in notes)
                {
                    Notes.Add(note);
                }
                await SaveStateAsync();
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorAsync($"Error filtering notes: {ex.Message}");
            }
            finally
            {
                await _uiService.HideLoadingAsync();
            }
        }

        private async Task SyncNotesAsync()
        {
            try
            {
                await _uiService.ShowLoadingAsync("Syncing notes...");
                var user = await _authService.GetCurrentUserAsync();
                foreach (var note in Notes)
                {
                    await _noteService.SyncWithGistAsync(note.Id);
                }
                await SaveStateAsync();
                await _uiService.ShowToastAsync("Notes synced successfully");
            }
            catch (Exception ex)
            {
                await _uiService.ShowErrorAsync($"Error syncing notes: {ex.Message}");
            }
            finally
            {
                await _uiService.HideLoadingAsync();
            }
        }
    }
}
