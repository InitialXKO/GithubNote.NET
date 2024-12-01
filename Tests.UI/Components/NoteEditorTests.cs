using Bunit;
using GithubNote.NET.Models;
using Moq;
using Xunit;

namespace GithubNote.NET.Tests.UI.Components
{
    public class NoteEditorTests : TestBase
    {
        public NoteEditorTests()
        {
            SetupDefaultMocks();
        }

        [Fact]
        public async Task NoteEditor_LoadsNote_WhenIdProvided()
        {
            // Arrange
            var note = new Note 
            { 
                Id = "1", 
                Title = "Test Note", 
                Content = "Test Content" 
            };

            MockNoteService
                .Setup(x => x.GetNoteAsync("1"))
                .ReturnsAsync(note);

            // Act
            var cut = RenderComponent<NoteEditor>(parameters => parameters
                .Add(p => p.NoteId, "1"));

            // Assert
            var titleInput = cut.Find(".note-title");
            var contentInput = cut.Find(".note-content");
            
            Assert.Equal("Test Note", titleInput.GetAttribute("value"));
            Assert.Equal("Test Content", contentInput.TextContent);
        }

        [Fact]
        public async Task NoteEditor_SavesNote_WhenSaveButtonClicked()
        {
            // Arrange
            var note = new Note 
            { 
                Id = "1", 
                Title = "Test Note", 
                Content = "Test Content" 
            };

            MockNoteService
                .Setup(x => x.SaveNoteAsync(It.IsAny<Note>()))
                .ReturnsAsync(note);

            // Act
            var cut = RenderComponent<NoteEditor>();
            
            var titleInput = cut.Find(".note-title");
            var contentInput = cut.Find(".note-content");
            var saveButton = cut.Find(".save-button");

            await cut.InvokeAsync(() => titleInput.Change("Test Note"));
            await cut.InvokeAsync(() => contentInput.Change("Test Content"));
            await cut.InvokeAsync(() => saveButton.Click());

            // Assert
            MockNoteService.Verify(x => x.SaveNoteAsync(It.Is<Note>(n => 
                n.Title == "Test Note" && 
                n.Content == "Test Content")), Times.Once);
        }

        [Fact]
        public void NoteEditor_ShowsEmptyEditor_WhenCreatingNewNote()
        {
            // Act
            var cut = RenderComponent<NoteEditor>(parameters => parameters
                .Add(p => p.IsNewNote, true));

            // Assert
            var titleInput = cut.Find(".note-title");
            var contentInput = cut.Find(".note-content");
            
            Assert.Equal("New Note", titleInput.GetAttribute("value"));
            Assert.Empty(contentInput.TextContent);
        }

        [Fact]
        public async Task NoteEditor_UpdatesUI_WhenNoteIsSaved()
        {
            // Arrange
            var savedNote = new Note 
            { 
                Id = "1", 
                Title = "Saved Note", 
                Content = "Saved Content",
                LastModified = DateTime.UtcNow
            };

            MockNoteService
                .Setup(x => x.SaveNoteAsync(It.IsAny<Note>()))
                .ReturnsAsync(savedNote);

            // Act
            var cut = RenderComponent<NoteEditor>();
            
            var titleInput = cut.Find(".note-title");
            var contentInput = cut.Find(".note-content");
            var saveButton = cut.Find(".save-button");

            await cut.InvokeAsync(() => titleInput.Change("Test Note"));
            await cut.InvokeAsync(() => contentInput.Change("Test Content"));
            await cut.InvokeAsync(() => saveButton.Click());

            // Assert
            var lastModifiedText = cut.Find(".last-modified");
            Assert.Contains(savedNote.LastModified.ToString(), lastModifiedText.TextContent);
            Assert.Equal("Saved Note", titleInput.GetAttribute("value"));
            Assert.Equal("Saved Content", contentInput.TextContent);
        }

        [Fact]
        public async Task NoteEditor_ShowsErrorMessage_WhenSaveFails()
        {
            // Arrange
            MockNoteService
                .Setup(x => x.SaveNoteAsync(It.IsAny<Note>()))
                .ThrowsAsync(new Exception("Save failed"));

            // Act
            var cut = RenderComponent<NoteEditor>();
            
            var titleInput = cut.Find(".note-title");
            var saveButton = cut.Find(".save-button");

            await cut.InvokeAsync(() => titleInput.Change("Test Note"));
            await cut.InvokeAsync(() => saveButton.Click());

            // Assert
            var errorMessage = cut.Find(".error-message");
            Assert.Contains("Failed to save note", errorMessage.TextContent);
        }
    }
}
