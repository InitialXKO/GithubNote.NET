using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GithubNote.NET.Models;

namespace GithubNote.NET.Services
{
    public interface INoteService
    {
        Task<Note> CreateNoteAsync(Note note);
        Task<Note> UpdateNoteAsync(Note note);
        Task<bool> DeleteNoteAsync(int noteId);
        Task<Note> GetNoteByIdAsync(int noteId);
        Task<IEnumerable<Note>> GetNotesByUserAsync(int userId);
        Task<IEnumerable<Note>> SearchNotesAsync(string query, int userId);
        Task<IEnumerable<Note>> GetNotesByCategoryAsync(string category, int userId);
        Task<bool> AddCategoryAsync(int noteId, string category);
        Task<bool> RemoveCategoryAsync(int noteId, string category);
        Task<IEnumerable<string>> GetUserCategoriesAsync(int userId);
        Task<Note> AddCommentAsync(int noteId, Comment comment);
        Task<Note> AddAttachmentAsync(int noteId, ImageAttachment attachment);
        Task<bool> SyncWithGistAsync(int noteId);
        Task<IEnumerable<Note>> GetRecentNotesAsync(int userId, int count = 10);
        Task<NoteMetadata> GetNoteMetadataAsync(int noteId);
        Task<bool> UpdateMetadataAsync(int noteId, NoteMetadata metadata);
    }
}
