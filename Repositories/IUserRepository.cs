using System.Threading.Tasks;
using GithubNote.NET.Models;

namespace GithubNote.NET.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByUsernameAsync(string username);
        Task<User> GetByEmailAsync(string email);
        Task<bool> UpdateSettingsAsync(string userId, UserSettings settings);
        Task<bool> UpdateGithubTokenAsync(string userId, string token);
        Task<bool> ValidateCredentialsAsync(string username, string password);
        Task<User> GetCurrentUserAsync();
        Task<bool> UpdateLastLoginTimeAsync(string userId);
    }
}
