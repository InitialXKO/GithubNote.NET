using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GithubNote.NET.Repositories
{
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        protected BaseRepository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await SaveChangesAsync();
            return entity;
        }

        public virtual async Task<T> UpdateAsync(T entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await SaveChangesAsync();
            return entity;
        }

        public virtual async Task DeleteAsync(string id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await SaveChangesAsync();
            }
        }

        public virtual async Task<bool> ExistsAsync(string id)
        {
            return await GetByIdAsync(id) != null;
        }

        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public virtual async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle concurrency conflicts
                throw new Exception("Concurrency conflict occurred while saving changes.", ex);
            }
            catch (DbUpdateException ex)
            {
                // Handle database update errors
                throw new Exception("Error occurred while saving changes to the database.", ex);
            }
        }

        protected virtual IQueryable<T> ApplyIncludes(IQueryable<T> query, params Expression<Func<T, object>>[] includes)
        {
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }
            return query;
        }
    }
}
