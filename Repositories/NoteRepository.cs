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
        private new readonly DbContext _context;

        public NoteRepository(DbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Note>> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> GetByCategoryAsync(string category)
        {
            return await _dbSet
                .Where(n => n.Categories.Contains(category))
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> SearchAsync(string searchTerm, string userId)
        {
            return await _dbSet
                .Where(n => n.UserId == userId &&
                           (n.Title.Contains(searchTerm) ||
                            n.Content.Contains(searchTerm) ||
                            n.Categories.Any(c => c.Contains(searchTerm)) ||
                            n.Tags.Any(t => t.Contains(searchTerm))))
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> GetNotesByTagAsync(string tag)
        {
            return await _dbSet
                .Where(n => n.Tags.Contains(tag))
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> SearchNotesAsync(string searchTerm)
        {
            return await _dbSet
                .Where(n => n.Title.Contains(searchTerm) || 
                           n.Content.Contains(searchTerm))
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> GetModifiedNotesAsync()
        {
            return await _dbSet
                .Where(n => n.UpdatedAt > n.LastSyncedAt)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> GetNotesByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _dbSet
                .Where(n => n.CreatedAt >= start && n.CreatedAt <= end)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Note> GetNoteWithDetailsAsync(string id)
        {
            return await _dbSet
                .Include(n => n.Comments)
                .Include(n => n.Attachments)
                .Include(n => n.Categories)
                .Include(n => n.Tags)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<bool> AddCommentAsync(string noteId, Comment comment)
        {
            var note = await GetNoteWithDetailsAsync(noteId);
            if (note == null) return false;

            note.Comments ??= new List<Comment>();
            note.Comments.Add(comment);
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddAttachmentAsync(string noteId, Attachment attachment)
        {
            var note = await GetNoteWithDetailsAsync(noteId);
            if (note == null) return false;

            note.Attachments ??= new List<Attachment>();
            note.Attachments.Add(attachment);
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateMetadataAsync(string noteId, NoteMetadata metadata)
        {
            var note = await GetByIdAsync(noteId);
            if (note == null) return false;

            note.Categories = metadata.Categories;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetAllTagsAsync()
        {
            return await _dbSet
                .SelectMany(n => n.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        public async Task<List<string>> GetAllCategoriesAsync()
        {
            return await _dbSet
                .SelectMany(n => n.Categories)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<int> GetNoteCountByTagAsync(string tag)
        {
            return await _dbSet
                .CountAsync(n => n.Tags.Contains(tag));
        }

        public async Task<int> GetNoteCountByCategoryAsync(string category)
        {
            return await _dbSet
                .CountAsync(n => n.Categories.Contains(category));
        }

        public async Task<List<Note>> GetRecentNotesAsync(string userId, int count)
        {
            return await _dbSet
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.UpdatedAt)
                .Take(count)
                .ToListAsync();
        }

        public override async Task<IEnumerable<Note>> GetAllAsync()
        {
            return await _dbSet
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> GetByCategoryAsync(string category, string userId)
        {
            return await _dbSet
                .Where(n => n.Categories.Contains(category) && n.UserId == userId)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }
    }
}
