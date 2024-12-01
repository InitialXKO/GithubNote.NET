using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using GithubNote.NET.Models;
using Microsoft.Extensions.Logging;
using GithubNote.NET.Services.UI.ErrorHandling;

namespace GithubNote.NET.Services.UI
{
    public interface INoteUIService
    {
        Task<List<Note>> GetNotesAsync();
        Task<Note> GetNoteAsync(string id);
        Task<Note> SaveNoteAsync(Note note);
        Task<bool> DeleteNoteAsync(string id);
        Task<List<Note>> SearchNotesAsync(string searchTerm);
        Task<Note> CreateNewNoteAsync();
    }

    public class NoteUIService : INoteUIService
    {
        private readonly INoteService _noteService;
        private readonly IUIStateManager _stateManager;
        private readonly ILogger<NoteUIService> _logger;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly IErrorHandlingService _errorHandling;
        private readonly IPerformanceOptimizer _optimizer;
        private readonly OptimizedNoteCache _noteCache;

        private const string CurrentNoteKey = "current_note";
        private const string NoteListKey = "note_list";
        private const string SearchResultsKey = "search_results";

        public NoteUIService(
            INoteService noteService,
            IUIStateManager stateManager,
            ILogger<NoteUIService> logger,
            IPerformanceMonitor performanceMonitor,
            IErrorHandlingService errorHandling,
            IPerformanceOptimizer optimizer,
            OptimizedNoteCache noteCache)
        {
            _noteService = noteService;
            _stateManager = stateManager;
            _logger = logger;
            _performanceMonitor = performanceMonitor;
            _errorHandling = errorHandling;
            _optimizer = optimizer;
            _noteCache = noteCache;
        }

        public async Task<List<Note>> GetNotesAsync()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // Try to get from cache first
                var notes = await _noteCache.GetNotesAsync();
                if (notes != null)
                {
                    await _stateManager.SetStateAsync(NoteListKey, notes);
                    return notes;
                }

                // If not in cache, get from service
                notes = await _noteService.GetNotesAsync();
                await _noteCache.SetNotesAsync(notes);
                await _stateManager.SetStateAsync(NoteListKey, notes);
                return notes;
            }
            catch (Exception ex)
            {
                await _errorHandling.HandleErrorAsync(ex, "NoteList", ErrorSeverity.Error);
                return new List<Note>();
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                _performanceMonitor.TrackOperation("NoteUIService.GetNotes", duration);
                await _optimizer.RecordMetricAsync("NoteUIService", duration, GetCurrentMemoryUsage());
            }
        }

        public async Task<Note> GetNoteAsync(string id)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // Try to get from cache first
                var note = await _noteCache.GetNoteAsync(id);
                if (note != null)
                {
                    await _stateManager.SetStateAsync(CurrentNoteKey, note);
                    return note;
                }

                // If not in cache, get from service
                note = await _noteService.GetNoteAsync(id);
                if (note != null)
                {
                    await _noteCache.SetNoteAsync(note);
                    await _stateManager.SetStateAsync(CurrentNoteKey, note);
                }
                else
                {
                    await _errorHandling.HandleErrorAsync(
                        $"Note with ID {id} not found",
                        "NoteEditor",
                        ErrorSeverity.Warning);
                }
                return note;
            }
            catch (Exception ex)
            {
                await _errorHandling.HandleErrorAsync(ex, "NoteEditor", ErrorSeverity.Error);
                return null;
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                _performanceMonitor.TrackOperation("NoteUIService.GetNote", duration);
                await _optimizer.RecordMetricAsync("NoteUIService", duration, GetCurrentMemoryUsage());
            }
        }

        public async Task<Note> SaveNoteAsync(Note note)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                if (string.IsNullOrWhiteSpace(note.Title))
                {
                    await _errorHandling.HandleErrorAsync(
                        "Note title cannot be empty",
                        "NoteEditor",
                        ErrorSeverity.Warning);
                    return null;
                }

                var savedNote = await _noteService.SaveNoteAsync(note);
                await _noteCache.SetNoteAsync(savedNote);
                await _stateManager.SetStateAsync(CurrentNoteKey, savedNote);

                var noteList = await _stateManager.GetStateAsync<List<Note>>(NoteListKey);
                if (noteList != null)
                {
                    var existingIndex = noteList.FindIndex(n => n.Id == savedNote.Id);
                    if (existingIndex >= 0)
                    {
                        noteList[existingIndex] = savedNote;
                    }
                    else
                    {
                        noteList.Add(savedNote);
                    }
                    await _stateManager.SetStateAsync(NoteListKey, noteList);
                    await _noteCache.SetNotesAsync(noteList);
                }

                return savedNote;
            }
            catch (Exception ex)
            {
                await _errorHandling.HandleErrorAsync(ex, "NoteEditor", ErrorSeverity.Error);
                return null;
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                _performanceMonitor.TrackOperation("NoteUIService.SaveNote", duration);
                await _optimizer.RecordMetricAsync("NoteUIService", duration, GetCurrentMemoryUsage());
            }
        }

        public async Task<bool> DeleteNoteAsync(string id)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var result = await _noteService.DeleteNoteAsync(id);
                if (result)
                {
                    await _stateManager.ClearStateAsync(CurrentNoteKey);
                    await _noteCache.InvalidateNoteAsync(id);

                    var noteList = await _stateManager.GetStateAsync<List<Note>>(NoteListKey);
                    if (noteList != null)
                    {
                        noteList.RemoveAll(n => n.Id == id);
                        await _stateManager.SetStateAsync(NoteListKey, noteList);
                        await _noteCache.SetNotesAsync(noteList);
                    }
                }
                else
                {
                    await _errorHandling.HandleErrorAsync(
                        $"Failed to delete note {id}",
                        "NoteList",
                        ErrorSeverity.Warning);
                }
                return result;
            }
            catch (Exception ex)
            {
                await _errorHandling.HandleErrorAsync(ex, "NoteList", ErrorSeverity.Error);
                return false;
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                _performanceMonitor.TrackOperation("NoteUIService.DeleteNote", duration);
                await _optimizer.RecordMetricAsync("NoteUIService", duration, GetCurrentMemoryUsage());
            }
        }

        public async Task<List<Note>> SearchNotesAsync(string searchTerm)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetNotesAsync();
                }

                // Get notes from cache if possible
                var allNotes = await _noteCache.GetNotesAsync() ?? await GetNotesAsync();
                
                var results = allNotes
                    .Where(n =>
                        n.Title?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                        n.Content?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                await _stateManager.SetStateAsync(SearchResultsKey, results);

                if (!results.Any())
                {
                    await _errorHandling.HandleErrorAsync(
                        $"No notes found matching '{searchTerm}'",
                        "NoteList",
                        ErrorSeverity.Info);
                }

                return results;
            }
            catch (Exception ex)
            {
                await _errorHandling.HandleErrorAsync(ex, "NoteList", ErrorSeverity.Error);
                return new List<Note>();
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                _performanceMonitor.TrackOperation("NoteUIService.SearchNotes", duration);
                await _optimizer.RecordMetricAsync("NoteUIService", duration, GetCurrentMemoryUsage());
            }
        }

        public async Task<Note> CreateNewNoteAsync()
        {
            var note = new Note
            {
                Id = Guid.NewGuid().ToString(),
                Title = "New Note",
                Content = "",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            await _stateManager.SetStateAsync(CurrentNoteKey, note);
            return note;
        }

        private long GetCurrentMemoryUsage()
        {
            return GC.GetTotalMemory(false);
        }
    }
}
