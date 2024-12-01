using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using GithubNote.NET.Models;
using GithubNote.NET.Cache;
using GithubNote.NET.Authentication;

namespace GithubNote.NET.Services
{
    public class NoteSyncService : INoteSync
    {
        private readonly IAuthenticationService _authService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<NoteSyncService> _logger;
        private readonly HttpClient _httpClient;
        private const string GistApiUrl = "https://api.github.com/gists";

        public NoteSyncService(
            IAuthenticationService authService,
            ICacheService cacheService,
            ILogger<NoteSyncService> logger,
            HttpClient httpClient)
        {
            _authService = authService;
            _cacheService = cacheService;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<string> CreateGistAsync(Note note)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var gistContent = new
                {
                    description = note.Title,
                    @public = false,
                    files = new Dictionary<string, object>
                    {
                        {
                            "note.md",
                            new { content = note.Content }
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(GistApiUrl, gistContent);
                response.EnsureSuccessStatusCode();

                var gistResponse = await response.Content.ReadFromJsonAsync<GistResponse>();
                await UpdateSyncStatus(note.Id, gistResponse.Id);

                _logger.LogInformation($"Created Gist for note {note.Id}");
                return gistResponse.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating Gist for note {note.Id}");
                throw;
            }
        }

        public async Task<bool> UpdateGistAsync(Note note, string gistId)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var gistContent = new
                {
                    description = note.Title,
                    files = new Dictionary<string, object>
                    {
                        {
                            "note.md",
                            new { content = note.Content }
                        }
                    }
                };

                var response = await _httpClient.PatchAsync(
                    $"{GistApiUrl}/{gistId}",
                    new StringContent(JsonSerializer.Serialize(gistContent)));
                
                response.EnsureSuccessStatusCode();
                await UpdateSyncStatus(note.Id, gistId);

                _logger.LogInformation($"Updated Gist {gistId} for note {note.Id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Gist {gistId} for note {note.Id}");
                throw;
            }
        }

        public async Task<Note> ImportFromGistAsync(string gistId, int userId)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{GistApiUrl}/{gistId}");
                response.EnsureSuccessStatusCode();

                var gist = await response.Content.ReadFromJsonAsync<GistResponse>();
                
                var note = new Note
                {
                    Title = gist.Description ?? "Imported Note",
                    Content = gist.Files["note.md"].Content,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"Imported note from Gist {gistId}");
                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing note from Gist {gistId}");
                throw;
            }
        }

        public async Task<bool> DeleteGistAsync(string gistId)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync($"{GistApiUrl}/{gistId}");
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"Deleted Gist {gistId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting Gist {gistId}");
                throw;
            }
        }

        public async Task<bool> SyncAllNotesAsync(int userId)
        {
            try
            {
                // TODO: 实现所有笔记的同步逻辑
                _logger.LogInformation($"Syncing all notes for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing all notes for user {userId}");
                throw;
            }
        }

        public async Task<SyncStatus> GetSyncStatusAsync(int noteId)
        {
            try
            {
                return await _cacheService.GetAsync<SyncStatus>($"sync_status_{noteId}") 
                    ?? new SyncStatus { IsSynced = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting sync status for note {noteId}");
                throw;
            }
        }

        private async Task<string> GetAuthTokenAsync()
        {
            var token = await _cacheService.GetAsync<string>("current_token");
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("User not authenticated");
            }
            return token;
        }

        private async Task UpdateSyncStatus(int noteId, string gistId)
        {
            var status = new SyncStatus
            {
                IsSynced = true,
                GistId = gistId,
                LastSyncTime = DateTime.UtcNow.ToString("o")
            };

            await _cacheService.SetAsync($"sync_status_{noteId}", status, TimeSpan.FromDays(1));
        }

        private class GistResponse
        {
            public string Id { get; set; }
            public string Description { get; set; }
            public Dictionary<string, GistFile> Files { get; set; }
        }

        private class GistFile
        {
            public string Filename { get; set; }
            public string Content { get; set; }
        }
    }
}
