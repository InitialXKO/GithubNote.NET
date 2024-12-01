using System;

namespace GithubNote.NET.Models
{
    public class ImageAttachment
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string LocalPath { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string NoteId { get; set; }

        public ImageAttachment()
        {
            Id = Guid.NewGuid().ToString();
            UploadedAt = DateTime.UtcNow;
        }
    }
}
