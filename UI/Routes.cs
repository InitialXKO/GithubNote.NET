namespace GithubNote.NET.UI
{
    public static class Routes
    {
        public const string NoteList = "//notes";
        public const string NoteEditor = "notes/editor";
        public const string Settings = "//settings";
        public const string About = "//about";
        
        public static string GetNoteEditorRoute(string? noteId = null)
        {
            return noteId == null ? NoteEditor : $"{NoteEditor}?noteId={noteId}";
        }
    }
}
