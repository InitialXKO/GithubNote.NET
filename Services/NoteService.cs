using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GithubNote.NET.Models;
using GithubNote.NET.Repositories;
using GithubNote.NET.Cache;
using GithubNote.NET.Authentication;

namespace GithubNote.NET.Services
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<NoteService> _logger;

        public NoteService(
            INoteRepository noteRepository,
            IUserRepository userRepository,
            ICacheService cacheService,
            IAuthenticationService authService,
            ILogger<NoteService> logger)
        {
            _noteRepository = noteRepository;
            _userRepository = userRepository;
            _cacheService = cacheService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<Note> CreateNoteAsync(Note note)
        {
            try
            {
                // 验证用户
                var user = await _userRepository.GetByIdAsync(note.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                // 设置创建时间
                note.CreatedAt = DateTime.UtcNow;
                note.UpdatedAt = note.CreatedAt;

                // 创建笔记
                var createdNote = await _noteRepository.AddAsync(note);

                // 更新缓存
                await UpdateNoteCache(createdNote);

                _logger.LogInformation($"Created note {createdNote.Id} for user {user.Id}");
                return createdNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating note for user {note.UserId}");
                throw;
            }
        }

        public async Task<Note> UpdateNoteAsync(Note note)
        {
            try
            {
                var existingNote = await _noteRepository.GetByIdAsync(note.Id);
                if (existingNote == null)
                {
                    throw new InvalidOperationException("Note not found");
                }

                // 更新时间
                note.UpdatedAt = DateTime.UtcNow;
                note.CreatedAt = existingNote.CreatedAt;

                // 更新笔记
                var updatedNote = await _noteRepository.UpdateAsync(note);

                // 更新缓存
                await UpdateNoteCache(updatedNote);

                _logger.LogInformation($"Updated note {updatedNote.Id}");
                return updatedNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating note {note.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteNoteAsync(string noteId)
        {
            try
            {
                var note = await _noteRepository.GetByIdAsync(noteId);
                if (note == null)
                {
                    return false;
                }

                // 删除笔记
                await _noteRepository.DeleteAsync(noteId);

                // 清除缓存
                await _cacheService.RemoveAsync($"note_{noteId}");
                await _cacheService.RemoveAsync($"note_metadata_{noteId}");

                _logger.LogInformation($"Deleted note {noteId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting note {noteId}");
                throw;
            }
        }

        public async Task<Note> GetNoteByIdAsync(string noteId)
        {
            return await GetNoteAsync(noteId);
        }

        public async Task<List<Note>> GetNotesByUserAsync(string userId)
        {
            try
            {
                var notes = await _noteRepository.GetByUserIdAsync(userId);
                foreach (var note in notes)
                {
                    await UpdateNoteCache(note);
                }
                return notes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving notes for user {userId}");
                throw;
            }
        }

        public async Task<List<Note>> SearchNotesAsync(string query, string userId)
        {
            try
            {
                return await _noteRepository.SearchAsync(query, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching notes for user {userId}");
                throw;
            }
        }

        public async Task<List<Note>> GetNotesByCategoryAsync(string category)
        {
            try
            {
                return await _noteRepository.GetByCategoryAsync(category);
                // Consider adding a new method in the repository if filtering by userId is needed.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving notes by category {category}");
                throw;
            }
        }

        public async Task<List<Note>> GetNotesByCategoryAsync(string category, string userId)
        {
            try
            {
                return await _noteRepository.GetByCategoryAsync(category, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving notes by category {category} for user {userId}");
                throw;
            }
        }

        public async Task<bool> AddCategoryAsync(string noteId, string category)
        {
            try
            {
                var note = await GetNoteByIdAsync(noteId);
                if (note == null)
                {
                    return false;
                }

                note.Categories ??= new List<string>();

                if (!note.Categories.Contains(category))
                {
                    note.Categories.Add(category);
                    await UpdateNoteAsync(note);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding category to note {noteId}");
                throw;
            }
        }

        public async Task<bool> RemoveCategoryAsync(string noteId, string category)
        {
            try
            {
                var note = await GetNoteByIdAsync(noteId);
                if (note?.Categories == null)
                {
                    return false;
                }

                if (note.Categories.Remove(category))
                {
                    await UpdateNoteAsync(note);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing category from note {noteId}");
                throw;
            }
        }

        public async Task<List<string>> GetUserCategoriesAsync(string userId)
        {
            try
            {
                var notes = await GetNotesByUserAsync(userId);
                return notes
                    .SelectMany(n => n.Categories ?? Enumerable.Empty<string>())
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving categories for user {userId}");
                throw;
            }
        }

        public async Task<Note> AddCommentAsync(string noteId, Comment comment)
        {
            try
            {
                var note = await _noteRepository.GetByIdAsync(noteId);
                if (note == null)
                {
                    throw new InvalidOperationException("Note not found");
                }

                if (note.Comments == null)
                {
                    note.Comments = new List<Comment>();
                }

                comment.CreatedAt = DateTime.UtcNow;
                note.Comments.Add(comment);
                return await UpdateNoteAsync(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding comment to note {noteId}");
                throw;
            }
        }

        public async Task<Note> AddAttachmentAsync(string noteId, Attachment attachment)
        {
            try
            {
                var note = await _noteRepository.GetByIdAsync(noteId);
                if (note == null)
                {
                    throw new InvalidOperationException("Note not found");
                }

                if (note.Attachments == null)
                {
                    note.Attachments = new List<Attachment>();
                }

                attachment.UploadedAt = DateTime.UtcNow;
                note.Attachments.Add(attachment);
                return await UpdateNoteAsync(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding attachment to note {noteId}");
                throw;
            }
        }

        public async Task<bool> SyncWithGistAsync(string noteId)
        {
            try
            {
                var note = await _noteRepository.GetByIdAsync(noteId);
                if (note == null)
                {
                    return false;
                }

                // TODO: 实现与GitHub Gist的同步逻辑
                _logger.LogInformation($"Syncing note {noteId} with GitHub Gist");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing note {noteId} with GitHub Gist");
                throw;
            }
        }

        public async Task<List<Note>> GetRecentNotesAsync(string userId, int count = 10)
        {
            try
            {
                return await _noteRepository.GetRecentNotesAsync(userId, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving recent notes for user {userId}");
                throw;
            }
        }

        public async Task<NoteMetadata> GetNoteMetadataAsync(string noteId)
        {
            try
            {
                // 尝试从缓存获取
                var cachedMetadata = await _cacheService.GetAsync<NoteMetadata>($"note_metadata_{noteId}");
                if (cachedMetadata != null)
                {
                    return cachedMetadata;
                }

                var note = await GetNoteByIdAsync(noteId);
                if (note == null)
                {
                    return null;
                }

                var metadata = new NoteMetadata
                {
                    NoteId = noteId,
                    LastModified = note.UpdatedAt,
                    CommentsCount = note.Comments?.Count ?? 0,
                    AttachmentsCount = note.Attachments?.Count ?? 0,
                    Categories = note.Categories?.ToList() ?? new List<string>()
                };

                // 缓存元数据
                await _cacheService.SetAsync($"note_metadata_{noteId}", metadata, TimeSpan.FromMinutes(30));

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving metadata for note {noteId}");
                throw;
            }
        }

        public async Task<bool> UpdateMetadataAsync(string noteId, NoteMetadata metadata)
        {
            try
            {
                var note = await GetNoteByIdAsync(noteId);
                if (note == null)
                {
                    return false;
                }

                note.Categories = metadata.Categories;
                await UpdateNoteAsync(note);

                // 更新缓存中的元数据
                await _cacheService.SetAsync($"note_metadata_{noteId}", metadata, TimeSpan.FromMinutes(30));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating metadata for note {noteId}");
                throw;
            }
        }

        public async Task<List<Note>> GetNotesAsync()
        {
            try
            {
                var notes = await _noteRepository.GetAllAsync();
                return notes.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all notes");
                throw;
            }
        }

        public async Task<Note> GetNoteAsync(string id)
        {
            try
            {
                var note = await _noteRepository.GetByIdAsync(id);
                if (note == null)
                {
                    _logger.LogWarning($"Note {id} not found");
                    return null;
                }
                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting note {id}");
                throw;
            }
        }

        public async Task<Note> SaveNoteAsync(Note note)
        {
            try
            {
                if (string.IsNullOrEmpty(note.Id))
                {
                    return await CreateNoteAsync(note);
                }

                note.UpdatedAt = DateTime.UtcNow;
                var updatedNote = await _noteRepository.UpdateAsync(note);
                await UpdateNoteCache(updatedNote);
                return updatedNote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving note {note.Id}");
                throw;
            }
        }

        private async Task UpdateNoteCache(Note note)
        {
            if (note != null)
            {
                await _cacheService.SetAsync($"note_{note.Id}", note, TimeSpan.FromMinutes(30));
            }
        }
    }
}
