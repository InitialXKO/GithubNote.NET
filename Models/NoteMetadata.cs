using System;
using System.Collections.Generic;

namespace GithubNote.NET.Models
{
    public class NoteMetadata
    {
        public string NoteId { get; set; }
        public string Version { get; set; }
        public string Editor { get; set; }
        public string Format { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; }
        public string LastSyncHash { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public SyncStatus SyncStatus { get; set; }
        public List<string> Categories { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }

        public NoteMetadata()
        {
            Version = "1.0";
            Format = "markdown";
            CustomProperties = new Dictionary<string, string>();
            Categories = new List<string>();
            SyncStatus = SyncStatus.NotSynced;
            LastModified = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            CommentsCount = 0;
            AttachmentsCount = 0;
        }
    }

    public enum SyncStatus
    {
        NotSynced,
        Synced,
        Modified,
        Conflicted
    }
}
