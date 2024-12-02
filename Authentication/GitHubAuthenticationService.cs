using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using GithubNote.NET.Models;
using GithubNote.NET.Cache;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace GithubNote.NET.Authentication
{
    public class GitHubAuthenticationService : IAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GitHubAuthenticationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private const string AuthBaseUrl = "https://github.com/login/oauth";
        private const string ApiBaseUrl = "https://api.github.com";

        public GitHubAuthenticationService(
            IConfiguration configuration,
            ICacheService cacheService,
            ILogger<GitHubAuthenticationService> logger,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _cacheService = cacheService;
            _logger = logger;
            _httpClient = httpClient;

            _clientId = configuration["GitHub:ClientId"];
            _clientSecret = configuration["GitHub:ClientSecret"];
            _redirectUri = configuration["GitHub:RedirectUri"];

            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                throw new InvalidOperationException("GitHub OAuth credentials not configured");
            }

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GithubNote.NET");
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string code)
        {
            try
            {
                // 交换授权码获取访问令牌
                var tokenResponse = await ExchangeCodeForTokenAsync(code);
                if (string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return new AuthenticationResult { Success = false, Error = "Failed to obtain access token" };
                }

                // 获取用户信息
                var user = await GetUserInfoAsync(tokenResponse.AccessToken);
                if (user == null)
                {
                    return new AuthenticationResult { Success = false, Error = "Failed to get user info" };
                }

                // 缓存令牌和用户信息
                await CacheAuthenticationDataAsync(tokenResponse, user);

                return new AuthenticationResult
                {
                    Success = true,
                    Token = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failed");
                return new AuthenticationResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<string> GetAuthorizationUrlAsync()
        {
            var state = GenerateState();
            await _cacheService.SetAsync($"auth_state_{state}", true, TimeSpan.FromMinutes(10));

            var scopes = "repo,gist,user";
            return $"{AuthBaseUrl}/authorize?" +
                   $"client_id={_clientId}&" +
                   $"redirect_uri={Uri.EscapeDataString(_redirectUri)}&" +
                   $"scope={Uri.EscapeDataString(scopes)}&" +
                   $"state={state}";
        }

        public async Task<User> GetCurrentUserAsync()
        {
            var token = await _cacheService.GetAsync<string>("current_token");
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            return await GetUserInfoAsync(token);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}/user");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task RevokeTokenAsync()
        {
            await _cacheService.RemoveAsync("current_token");
            await _cacheService.RemoveAsync("current_user");
            // GitHub的OAuth不支持直接撤销令牌
        }

        public async Task<bool> RefreshTokenAsync()
        {
            var refreshToken = await _cacheService.GetAsync<string>("refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{AuthBaseUrl}/access_token", new
                {
                    client_id = _clientId,
                    client_secret = _clientSecret,
                    refresh_token = refreshToken,
                    grant_type = "refresh_token"
                });

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
                await _cacheService.SetAsync("current_token", tokenResponse.AccessToken);
                await _cacheService.SetAsync("refresh_token", tokenResponse.RefreshToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await _cacheService.GetAsync<string>("current_token");
            return !string.IsNullOrEmpty(token) && await ValidateTokenAsync(token);
        }

        private async Task<TokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            var response = await _httpClient.PostAsJsonAsync($"{AuthBaseUrl}/access_token", new
            {
                client_id = _clientId,
                client_secret = _clientSecret,
                code,
                redirect_uri = _redirectUri
            });

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TokenResponse>();
        }

        private async Task<User> GetUserInfoAsync(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/user");
            response.EnsureSuccessStatusCode();
            
            var githubUser = await response.Content.ReadFromJsonAsync<GitHubUser>();
            return MapToUser(githubUser, token);
        }

        private async Task CacheAuthenticationDataAsync(TokenResponse tokenResponse, User user)
        {
            var options = new TimeSpan(7, 0, 0, 0); // 7 days
            await _cacheService.SetAsync("current_token", tokenResponse.AccessToken, options);
            await _cacheService.SetAsync("refresh_token", tokenResponse.RefreshToken, options);
            await _cacheService.SetAsync("current_user", user, options);
        }

        private string GenerateState()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        private User MapToUser(GitHubUser githubUser, string token)
        {
            return new User
            {
                Username = githubUser.Login,
                Email = githubUser.Email,
                AvatarUrl = githubUser.AvatarUrl,
                GithubToken = token,
                LastLoginTime = DateTime.UtcNow
            };
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }

            [JsonPropertyName("scope")]
            public string Scope { get; set; }
        }

        private class GitHubUser
        {
            [JsonPropertyName("login")]
            public string Login { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }

            [JsonPropertyName("avatar_url")]
            public string AvatarUrl { get; set; }
        }
    }
}
