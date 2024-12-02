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

        public async Task<Note> CreateNoteAsync(Note note)
        {
            var createdNote = await _innerService.CreateNoteAsync(note);
            await InvalidateListCache();
            return createdNote;
        }

        public async Task<Note> UpdateNoteAsync(Note note)
        {
            var updatedNote = await _innerService.UpdateNoteAsync(note);
            await InvalidateNoteCache(note.Id);
            await InvalidateListCache();
            return updatedNote;
        }

        public async Task<bool> DeleteNoteAsync(int noteId)
        {
            var result = await _innerService.DeleteNoteAsync(noteId);
            if (result)
            {
                await InvalidateNoteCache(noteId);
                await InvalidateListCache();
            }
            return result;
        }

        public async Task<Note> GetNoteByIdAsync(int noteId)
        {
            return await _cacheService.GetOrAddAsync(
                $"{NoteCachePrefix}{noteId}",
                () => _innerService.GetNoteByIdAsync(noteId),
                _noteExpiration);
        }

        public async Task<IEnumerable<Note>> GetNotesByUserAsync(int userId)
        {
            return await _cacheService.GetOrAddAsync(
                $"{NoteListCacheKey}_{userId}",
                () => _innerService.GetNotesByUserAsync(userId),
                _listExpiration);
        }

        public async Task<IEnumerable<Note>> SearchNotesAsync(string query, int userId)
        {
            // Don't cache search results
            return await _innerService.SearchNotesAsync(query, userId);
        }

        public async Task<IEnumerable<Note>> GetNotesByCategoryAsync(string category, int userId)
        {
            return await _cacheService.GetOrAddAsync(
                $"{NoteListCacheKey}_{userId}_{category}",
                () => _innerService.GetNotesByCategoryAsync(category, userId),
                _listExpiration);
        }

        public async Task<bool> AddCategoryAsync(int noteId, string category)
        {
            var result = await _innerService.AddCategoryAsync(noteId, category);
            if (result)
            {
                await InvalidateNoteCache(noteId);
                await InvalidateListCache();
            }
            return result;
        }

        public async Task<bool> RemoveCategoryAsync(int noteId, string category)
        {
            var result = await _innerService.RemoveCategoryAsync(noteId, category);
            if (result)
            {
                await InvalidateNoteCache(noteId);
                await InvalidateListCache();
            }
            return result;
        }

        public async Task<IEnumerable<string>> GetUserCategoriesAsync(int userId)
        {
            return await _cacheService.GetOrAddAsync(
                $"categories_{userId}",
                () => _innerService.GetUserCategoriesAsync(userId),
                _listExpiration);
        }

        public async Task<Note> AddCommentAsync(int noteId, Comment comment)
        {
            var updatedNote = await _innerService.AddCommentAsync(noteId, comment);
            await InvalidateNoteCache(noteId);
            return updatedNote;
        }

        public async Task<Note> AddAttachmentAsync(int noteId, ImageAttachment attachment)
        {
            var updatedNote = await _innerService.AddAttachmentAsync(noteId, attachment);
            await InvalidateNoteCache(noteId);
            return updatedNote;
        }

        public async Task<bool> SyncWithGistAsync(int userId)
        {
            var result = await _innerService.SyncWithGistAsync(userId);
            if (result)
            {
                await InvalidateListCache();
            }
            return result;
        }

        public async Task<IEnumerable<Note>> GetRecentNotesAsync(int userId, int count)
        {
            return await _cacheService.GetOrAddAsync(
                $"{NoteListCacheKey}_recent_{userId}_{count}",
                () => _innerService.GetRecentNotesAsync(userId, count),
                _listExpiration);
        }

        public async Task<NoteMetadata> GetNoteMetadataAsync(int noteId)
        {
            return await _cacheService.GetOrAddAsync(
                $"{NoteCachePrefix}metadata_{noteId}",
                () => _innerService.GetNoteMetadataAsync(noteId),
                _noteExpiration);
        }

        public async Task<bool> UpdateMetadataAsync(int noteId, NoteMetadata metadata)
        {
            var result = await _innerService.UpdateMetadataAsync(noteId, metadata);
            if (result)
            {
                await InvalidateNoteCache(noteId);
            }
            return result;
        }

        private async Task InvalidateNoteCache(int noteId)
        {
            await _cacheService.RemoveAsync($"{NoteCachePrefix}{noteId}");
        }

        private async Task InvalidateListCache()
        {
            await _cacheService.RemoveAsync(NoteListCacheKey);
        }
    }
}
