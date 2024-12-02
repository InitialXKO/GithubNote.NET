using System;

namespace GithubNote.NET.Models
{
    public class Attachment
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string MimeType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public string Description { get; set; }

        public Attachment()
        {
            Id = Guid.NewGuid().ToString();
            UploadedAt = DateTime.UtcNow;
        }
    }
}
