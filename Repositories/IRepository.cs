using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace GithubNote.NET.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(string id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<int> CountAsync();
        Task SaveChangesAsync();
    }
}
