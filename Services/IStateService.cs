using System.Threading.Tasks;

namespace GithubNote.NET.Services
{
    public interface IStateService
    {
        Task SaveStateAsync<T>(string key, T state);
        Task<T> LoadStateAsync<T>(string key);
        Task ClearStateAsync(string key);
        Task<bool> HasStateAsync(string key);
    }
}
