using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GithubNote.NET.Models;

namespace GithubNote.NET.Repositories
{
    public interface INoteRepository : IRepository<Note>
    {
        Task<List<Note>> GetByUserIdAsync(string userId);
        Task<List<Note>> GetByCategoryAsync(string category);
        Task<List<Note>> GetByCategoryAsync(string category, string userId);
        Task<List<Note>> SearchAsync(string searchTerm, string userId);
        Task<List<Note>> GetNotesByTagAsync(string tag);
        Task<List<Note>> SearchNotesAsync(string searchTerm);
        Task<List<Note>> GetModifiedNotesAsync();
        Task<List<Note>> GetNotesByDateRangeAsync(DateTime start, DateTime end);
        Task<Note> GetNoteWithDetailsAsync(string id);
        Task<bool> AddCommentAsync(string noteId, Comment comment);
        Task<bool> AddAttachmentAsync(string noteId, Attachment attachment);
        Task<bool> UpdateMetadataAsync(string noteId, NoteMetadata metadata);
        Task<List<string>> GetAllTagsAsync();
        Task<List<string>> GetAllCategoriesAsync();
        Task<int> GetNoteCountByTagAsync(string tag);
        Task<int> GetNoteCountByCategoryAsync(string category);
        Task<List<Note>> GetRecentNotesAsync(string userId, int count);
    }
}
