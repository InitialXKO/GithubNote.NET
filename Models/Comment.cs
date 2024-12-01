using System;

namespace GithubNote.NET.Models
{
    public class Comment
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string NoteId { get; set; }

        public Comment()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
