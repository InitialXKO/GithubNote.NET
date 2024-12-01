using System.Threading.Tasks;
using GithubNote.NET.Models;

namespace GithubNote.NET.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> AuthenticateAsync(string code);
        Task<string> GetAuthorizationUrlAsync();
        Task<User> GetCurrentUserAsync();
        Task<bool> ValidateTokenAsync(string token);
        Task RevokeTokenAsync();
        Task<bool> RefreshTokenAsync();
        Task<bool> IsAuthenticatedAsync();
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public User User { get; set; }
        public string Error { get; set; }
    }
}
