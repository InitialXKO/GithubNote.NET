using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GithubNote.NET.Models;

namespace GithubNote.NET.Services
{
    public interface INoteService
    {
        Task<List<Note>> GetNotesAsync();
        Task<Note> GetNoteAsync(string noteId);
        Task<Note> SaveNoteAsync(Note note);
        Task<Note> CreateNoteAsync(Note note);
        Task<Note> UpdateNoteAsync(Note note);
        Task<bool> DeleteNoteAsync(string noteId);
        Task<Note> GetNoteByIdAsync(string noteId);
        Task<List<Note>> GetNotesByUserAsync(string userId);
        Task<List<Note>> SearchNotesAsync(string query, string userId);
        Task<List<Note>> GetNotesByCategoryAsync(string category, string userId);
        Task<bool> AddCategoryAsync(string noteId, string category);
        Task<bool> RemoveCategoryAsync(string noteId, string category);
        Task<List<string>> GetUserCategoriesAsync(string userId);
        Task<Note> AddCommentAsync(string noteId, Comment comment);
        Task<Note> AddAttachmentAsync(string noteId, Attachment attachment);
        Task<bool> SyncWithGistAsync(string noteId);
        Task<List<Note>> GetRecentNotesAsync(string userId, int count = 10);
        Task<NoteMetadata> GetNoteMetadataAsync(string noteId);
        Task<bool> UpdateMetadataAsync(string noteId, NoteMetadata metadata);
    }
}
