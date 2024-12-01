using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GithubNote.NET.Models;

namespace GithubNote.NET.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(DbContext context) : base(context)
        {
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _dbSet
                .Include(u => u.Settings)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _dbSet
                .Include(u => u.Settings)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UpdateSettingsAsync(string userId, UserSettings settings)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return false;

            user.Settings = settings;
            await SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateGithubTokenAsync(string userId, string token)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return false;

            user.GithubToken = token;
            await SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            // 实际项目中应该使用密码哈希和安全验证
            var user = await GetByUsernameAsync(username);
            return user != null;
        }

        public async Task<User> GetCurrentUserAsync()
        {
            // 这里应该从当前会话或认证上下文中获取用户ID
            // 示例实现
            throw new NotImplementedException("需要实现用户认证机制");
        }

        public async Task<bool> UpdateLastLoginTimeAsync(string userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return false;

            user.LastLoginTime = DateTime.UtcNow;
            await SaveChangesAsync();
            return true;
        }

        public override async Task<User> AddAsync(User user)
        {
            // 添加用户前的验证
            var existingUser = await GetByUsernameAsync(user.Username);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Username already exists");
            }

            existingUser = await GetByEmailAsync(user.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            return await base.AddAsync(user);
        }
    }
}
