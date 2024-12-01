using System.Collections.Generic;

namespace GithubNote.NET.UI.ViewModels.States
{
    public class NoteEditorState
    {
        public string NoteId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<string> Categories { get; set; }
        public bool IsDirty { get; set; }
        public string LastEditPosition { get; set; }
        public string DraftContent { get; set; }
    }
}
