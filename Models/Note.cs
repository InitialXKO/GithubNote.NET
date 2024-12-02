using System;
using System.Collections.Generic;

namespace GithubNote.NET.Models
{
    public class Note
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string GistId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsPrivate { get; set; }
        public string UserId { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Categories { get; set; }
        public List<Comment> Comments { get; set; }
        public List<ImageAttachment> Images { get; set; }
        public List<Attachment> Attachments { get; set; }
        public NoteMetadata Metadata { get; set; }
        public bool IsSynced { get; set; }
        public DateTime? LastSyncedAt { get; set; }

        public Note()
        {
            Id = Guid.NewGuid().ToString();
            Tags = new List<string>();
            Categories = new List<string>();
            Comments = new List<Comment>();
            Images = new List<ImageAttachment>();
            Attachments = new List<Attachment>();
            Metadata = new NoteMetadata();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsSynced = false;
        }

        public DateTime Created => CreatedAt;
        public DateTime LastModified => UpdatedAt;
    }
}
