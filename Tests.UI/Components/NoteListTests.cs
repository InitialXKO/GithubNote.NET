using Bunit;
using GithubNote.NET.Models;
using Moq;
using Xunit;
using System.Collections.Generic;

namespace GithubNote.NET.Tests.UI.Components
{
    public class NoteListTests : TestBase
    {
        public NoteListTests()
        {
            SetupDefaultMocks();
        }

        [Fact]
        public async Task NoteList_DisplaysNotes_WhenNotesExist()
        {
            // Arrange
            var notes = new List<Note>
            {
                new Note { Id = "1", Title = "Test Note 1", Content = "Content 1" },
                new Note { Id = "2", Title = "Test Note 2", Content = "Content 2" }
            };

            MockNoteService
                .Setup(x => x.GetNotesAsync())
                .ReturnsAsync(notes);

            // Act
            var cut = RenderComponent<NoteList>();
            await cut.InvokeAsync(() => NoteUIService.GetNotesAsync());

            // Assert
            var noteElements = cut.FindAll(".note-item");
            Assert.Equal(2, noteElements.Count);
            Assert.Contains(noteElements, e => e.TextContent.Contains("Test Note 1"));
            Assert.Contains(noteElements, e => e.TextContent.Contains("Test Note 2"));
        }

        [Fact]
        public async Task NoteList_ShowsEmptyMessage_WhenNoNotes()
        {
            // Arrange
            MockNoteService
                .Setup(x => x.GetNotesAsync())
                .ReturnsAsync(new List<Note>());

            // Act
            var cut = RenderComponent<NoteList>();
            await cut.InvokeAsync(() => NoteUIService.GetNotesAsync());

            // Assert
            var emptyMessage = cut.Find(".empty-message");
            Assert.Contains("No notes found", emptyMessage.TextContent);
        }

        [Fact]
        public async Task NoteList_DeletesNote_WhenDeleteButtonClicked()
        {
            // Arrange
            var notes = new List<Note>
            {
                new Note { Id = "1", Title = "Test Note 1", Content = "Content 1" }
            };

            MockNoteService
                .Setup(x => x.GetNotesAsync())
                .ReturnsAsync(notes);

            MockNoteService
                .Setup(x => x.DeleteNoteAsync("1"))
                .ReturnsAsync(true);

            // Act
            var cut = RenderComponent<NoteList>();
            await cut.InvokeAsync(() => NoteUIService.GetNotesAsync());
            
            var deleteButton = cut.Find(".delete-button");
            await cut.InvokeAsync(() => deleteButton.Click());

            // Assert
            MockNoteService.Verify(x => x.DeleteNoteAsync("1"), Times.Once);
            MockStateManager.Verify(
                x => x.ClearStateAsync(It.Is<string>(s => s.Contains("current_note"))), 
                Times.Once);
        }

        [Fact]
        public async Task NoteList_SearchesNotes_WhenSearchTermEntered()
        {
            // Arrange
            var allNotes = new List<Note>
            {
                new Note { Id = "1", Title = "Test Note 1", Content = "Content 1" },
                new Note { Id = "2", Title = "Another Note", Content = "Content 2" }
            };

            MockNoteService
                .Setup(x => x.GetNotesAsync())
                .ReturnsAsync(allNotes);

            // Act
            var cut = RenderComponent<NoteList>();
            var searchInput = cut.Find(".search-input");
            await cut.InvokeAsync(() => searchInput.Change("Test"));

            // Assert
            var noteElements = cut.FindAll(".note-item");
            Assert.Single(noteElements);
            Assert.Contains(noteElements, e => e.TextContent.Contains("Test Note 1"));
            Assert.DoesNotContain(noteElements, e => e.TextContent.Contains("Another Note"));
        }
    }
}
