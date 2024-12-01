using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using GithubNote.NET.Models;

namespace GithubNote.NET.Repositories
{
    public class NoteRepository : BaseRepository<Note>, INoteRepository
    {
        public NoteRepository(DbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Note>> GetNotesByTagAsync(string tag)
        {
            return await _dbSet
                .Where(n => n.Tags.Contains(tag))
                .ToListAsync();
        }

        public async Task<IEnumerable<Note>> SearchNotesAsync(string searchTerm)
        {
            return await _dbSet
                .Where(n => n.Title.Contains(searchTerm) || n.Content.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<IEnumerable<Note>> GetModifiedNotesAsync()
        {
            return await _dbSet
                .Where(n => n.Metadata.SyncStatus == SyncStatus.Modified)
                .ToListAsync();
        }

        public async Task<IEnumerable<Note>> GetNotesByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _dbSet
                .Where(n => n.CreatedAt >= start && n.CreatedAt <= end)
                .ToListAsync();
        }

        public async Task<Note> GetNoteWithDetailsAsync(string id)
        {
            return await _dbSet
                .Include(n => n.Comments)
                .Include(n => n.Images)
                .Include(n => n.Metadata)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<bool> AddCommentAsync(string noteId, Comment comment)
        {
            var note = await GetNoteWithDetailsAsync(noteId);
            if (note == null) return false;

            note.Comments.Add(comment);
            await SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddImageAsync(string noteId, ImageAttachment image)
        {
            var note = await GetNoteWithDetailsAsync(noteId);
            if (note == null) return false;

            note.Images.Add(image);
            await SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateMetadataAsync(string noteId, NoteMetadata metadata)
        {
            var note = await GetByIdAsync(noteId);
            if (note == null) return false;

            note.Metadata = metadata;
            await SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<string>> GetAllTagsAsync()
        {
            return await _dbSet
                .SelectMany(n => n.Tags)
                .Distinct()
                .ToListAsync();
        }

        public async Task<int> GetNoteCountByTagAsync(string tag)
        {
            return await _dbSet
                .CountAsync(n => n.Tags.Contains(tag));
        }
    }
}
