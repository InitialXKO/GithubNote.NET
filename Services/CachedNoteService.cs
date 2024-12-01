using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GithubNote.NET.Models;
using GithubNote.NET.Cache;

namespace GithubNote.NET.Services
{
    public class CachedNoteService : INoteService
    {
        private readonly INoteService _innerService;
        private readonly ICacheService _cacheService;
        private const string NoteCachePrefix = "note_";
        private const string NoteListCacheKey = "notes_list";
        private readonly TimeSpan _noteExpiration = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _listExpiration = TimeSpan.FromMinutes(5);

        public CachedNoteService(
            INoteService innerService,
            ICacheService cacheService)
        {
            _innerService = innerService;
            _cacheService = cacheService;
        }

        public async Task<Note> GetNoteAsync(string id)
        {
            return await _cacheService.GetOrAddAsync(
                $"{NoteCachePrefix}{id}",
                () => _innerService.GetNoteAsync(id),
                _noteExpiration);
        }

        public async Task<List<Note>> GetNotesAsync()
        {
            return await _cacheService.GetOrAddAsync(
                NoteListCacheKey,
                () => _innerService.GetNotesAsync(),
                _listExpiration);
        }

        public async Task<Note> SaveNoteAsync(Note note)
        {
            var savedNote = await _innerService.SaveNoteAsync(note);
            await InvalidateNoteCache(note.Id);
            return savedNote;
        }

        public async Task<bool> DeleteNoteAsync(string id)
        {
            var result = await _innerService.DeleteNoteAsync(id);
            if (result)
            {
                await InvalidateNoteCache(id);
            }
            return result;
        }

        public async Task<List<Note>> SearchNotesAsync(string query)
        {
            // Don't cache search results as they might change frequently
            return await _innerService.SearchNotesAsync(query);
        }

        private async Task InvalidateNoteCache(string id)
        {
            await _cacheService.RemoveAsync($"{NoteCachePrefix}{id}");
            await _cacheService.RemoveAsync(NoteListCacheKey);
        }
    }
}
