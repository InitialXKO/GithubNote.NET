using System.Collections.Generic;
using GithubNote.NET.Models;

namespace GithubNote.NET.UI.ViewModels.States
{
    public class NoteListState
    {
        public List<Note> Notes { get; set; }
        public string SearchQuery { get; set; }
        public string SelectedCategory { get; set; }
        public string ScrollPosition { get; set; }
        public bool IsSearchActive { get; set; }
        public string LastSyncTime { get; set; }
    }
}
