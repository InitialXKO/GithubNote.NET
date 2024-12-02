using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GithubNote.NET.Models;

namespace GithubNote.NET.Data.Repositories
{
    public interface INoteRepository
    {
        Task<Note> GetByIdAsync(int id);
        Task<IEnumerable<Note>> GetAllAsync();
        Task<Note> AddAsync(Note note);
        Task UpdateAsync(Note note);
        Task DeleteAsync(int id);
        Task<IEnumerable<Note>> SearchAsync(string searchTerm);
    }
}
