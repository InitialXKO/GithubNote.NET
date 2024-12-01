using System;
using System.Collections.Generic;

namespace GithubNote.NET.Models
{
    public class NoteMetadata
    {
        public string Version { get; set; }
        public string Editor { get; set; }
        public string Format { get; set; }
        public Dictionary<string, string> CustomProperties { get; set; }
        public string LastSyncHash { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public SyncStatus SyncStatus { get; set; }

        public NoteMetadata()
        {
            Version = "1.0";
            Format = "markdown";
            CustomProperties = new Dictionary<string, string>();
            SyncStatus = SyncStatus.NotSynced;
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
