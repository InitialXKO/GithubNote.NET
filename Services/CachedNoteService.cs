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
        private const string UserNotesPrefix = "user_notes_";
        private const string CategoryNotesPrefix = "category_notes_";
        private readonly TimeSpan _noteExpiration = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _listExpiration = TimeSpan.FromMinutes(5);

        public CachedNoteService(
            INoteService innerService,
            ICacheService cacheService)
        {
            _innerService = innerService;
            _cacheService = cacheService;
        }

        public async Task<List<Note>> GetNotesAsync()
        {
            var cacheKey = NoteListCacheKey;
            var notes = await _cacheService.GetAsync<List<Note>>(cacheKey);
            if (notes == null)
            {
                notes = await _innerService.GetNotesAsync();
                await _cacheService.SetAsync(cacheKey, notes, _listExpiration);
            }
            return notes;
        }

        public async Task<Note> GetNoteAsync(string noteId)
        {
            var cacheKey = $"{NoteCachePrefix}{noteId}";
            var note = await _cacheService.GetAsync<Note>(cacheKey);
            if (note == null)
            {
                note = await _innerService.GetNoteAsync(noteId);
                if (note != null)
                {
                    await _cacheService.SetAsync(cacheKey, note, _noteExpiration);
                }
            }
            return note;
        }

        public async Task<Note> SaveNoteAsync(Note note)
        {
            var savedNote = await _innerService.SaveNoteAsync(note);
            await InvalidateNoteCache(note.Id);
            await InvalidateListCache();
            return savedNote;
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

        public async Task<bool> DeleteNoteAsync(string noteId)
        {
            var result = await _innerService.DeleteNoteAsync(noteId);
            if (result)
            {
                await InvalidateNoteCache(noteId);
                await InvalidateListCache();
            }
            return result;
        }

        public async Task<Note> GetNoteByIdAsync(string noteId)
        {
            return await GetNoteAsync(noteId);
        }

        public async Task<List<Note>> GetNotesByUserAsync(string userId)
        {
            var cacheKey = $"{UserNotesPrefix}{userId}";
            var notes = await _cacheService.GetAsync<List<Note>>(cacheKey);
            if (notes == null)
            {
                notes = await _innerService.GetNotesByUserAsync(userId);
                await _cacheService.SetAsync(cacheKey, notes, _listExpiration);
            }
            return notes;
        }

        public async Task<List<Note>> SearchNotesAsync(string query, string userId)
        {
            // Search results are not cached as they may change frequently
            return await _innerService.SearchNotesAsync(query, userId);
        }

        public async Task<List<Note>> GetNotesByCategoryAsync(string category, string userId)
        {
            var cacheKey = $"{CategoryNotesPrefix}{category}_{userId}";
            var notes = await _cacheService.GetAsync<List<Note>>(cacheKey);
            if (notes == null)
            {
                notes = await _innerService.GetNotesByCategoryAsync(category, userId);
                await _cacheService.SetAsync(cacheKey, notes, _listExpiration);
            }
            return notes;
        }

        public async Task<bool> AddCategoryAsync(string noteId, string category)
        {
            var result = await _innerService.AddCategoryAsync(noteId, category);
            if (result)
            {
                await InvalidateNoteCache(noteId);
                await InvalidateListCache();
            }
            return result;
        }

        public async Task<bool> RemoveCategoryAsync(string noteId, string category)
        {
            var result = await _innerService.RemoveCategoryAsync(noteId, category);
            if (result)
            {
                await InvalidateNoteCache(noteId);
                await InvalidateListCache();
            }
            return result;
        }

        public async Task<List<string>> GetUserCategoriesAsync(string userId)
        {
            var cacheKey = $"user_categories_{userId}";
            var categories = await _cacheService.GetAsync<List<string>>(cacheKey);
            if (categories == null)
            {
                categories = await _innerService.GetUserCategoriesAsync(userId);
                await _cacheService.SetAsync(cacheKey, categories, _listExpiration);
            }
            return categories;
        }

        public async Task<Note> AddCommentAsync(string noteId, Comment comment)
        {
            var updatedNote = await _innerService.AddCommentAsync(noteId, comment);
            await InvalidateNoteCache(noteId);
            return updatedNote;
        }

        public async Task<Note> AddAttachmentAsync(string noteId, Attachment attachment)
        {
            var updatedNote = await _innerService.AddAttachmentAsync(noteId, attachment);
            await InvalidateNoteCache(noteId);
            return updatedNote;
        }

        public async Task<bool> SyncWithGistAsync(string noteId)
        {
            var result = await _innerService.SyncWithGistAsync(noteId);
            if (result)
            {
                await InvalidateNoteCache(noteId);
            }
            return result;
        }

        public async Task<List<Note>> GetRecentNotesAsync(string userId, int count = 10)
        {
            var cacheKey = $"recent_notes_{userId}_{count}";
            var notes = await _cacheService.GetAsync<List<Note>>(cacheKey);
            if (notes == null)
            {
                notes = await _innerService.GetRecentNotesAsync(userId, count);
                await _cacheService.SetAsync(cacheKey, notes, _listExpiration);
            }
            return notes;
        }

        public async Task<NoteMetadata> GetNoteMetadataAsync(string noteId)
        {
            var cacheKey = $"metadata_{noteId}";
            var metadata = await _cacheService.GetAsync<NoteMetadata>(cacheKey);
            if (metadata == null)
            {
                metadata = await _innerService.GetNoteMetadataAsync(noteId);
                if (metadata != null)
                {
                    await _cacheService.SetAsync(cacheKey, metadata, _noteExpiration);
                }
            }
            return metadata;
        }

        public async Task<bool> UpdateMetadataAsync(string noteId, NoteMetadata metadata)
        {
            var result = await _innerService.UpdateMetadataAsync(noteId, metadata);
            if (result)
            {
                await InvalidateNoteCache(noteId);
                var metadataCacheKey = $"metadata_{noteId}";
                await _cacheService.RemoveAsync(metadataCacheKey);
            }
            return result;
        }

        private async Task InvalidateNoteCache(string noteId)
        {
            var cacheKey = $"{NoteCachePrefix}{noteId}";
            await _cacheService.RemoveAsync(cacheKey);
        }

        private async Task InvalidateListCache()
        {
            await _cacheService.RemoveAsync(NoteListCacheKey);
        }
    }
}
