using System.Threading.Tasks;
using GithubNote.NET.Models;

namespace GithubNote.NET.Services
{
    public interface INoteSync
    {
        Task<string> CreateGistAsync(Note note);
        Task<bool> UpdateGistAsync(Note note, string gistId);
        Task<Note> ImportFromGistAsync(string gistId, int userId);
        Task<bool> DeleteGistAsync(string gistId);
        Task<bool> SyncAllNotesAsync(int userId);
        Task<SyncStatus> GetSyncStatusAsync(string noteId);
    }

    public class SyncStatus
    {
        public bool IsSynced { get; set; }
        public string GistId { get; set; }
        public string LastSyncTime { get; set; }
        public string LastError { get; set; }
    }
}
