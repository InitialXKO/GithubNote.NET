using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GithubNote.NET.Models;

namespace GithubNote.NET.Repositories
{
    public interface INoteRepository : IRepository<Note>
    {
        Task<IEnumerable<Note>> GetNotesByTagAsync(string tag);
        Task<IEnumerable<Note>> SearchNotesAsync(string searchTerm);
        Task<IEnumerable<Note>> GetModifiedNotesAsync();
        Task<IEnumerable<Note>> GetNotesByDateRangeAsync(DateTime start, DateTime end);
        Task<Note> GetNoteWithDetailsAsync(string id);
        Task<bool> AddCommentAsync(string noteId, Comment comment);
        Task<bool> AddImageAsync(string noteId, ImageAttachment image);
        Task<bool> UpdateMetadataAsync(string noteId, NoteMetadata metadata);
        Task<IEnumerable<string>> GetAllTagsAsync();
        Task<int> GetNoteCountByTagAsync(string tag);
    }
}
