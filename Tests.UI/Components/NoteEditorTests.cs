using Microsoft.Maui.Controls.Testing;
using Moq;
using Xunit;
using GithubNote.NET.Services;
using GithubNote.NET.UI.Controls;
using GithubNote.NET.Models;

namespace GithubNote.NET.Tests.UI.Components
{
    public class NoteEditorTests : IClassFixture<MauiAppFixture>
    {
        private readonly Mock<INoteService> _noteServiceMock;
        private readonly Mock<IThemeService> _themeServiceMock;

        public NoteEditorTests()
        {
            _noteServiceMock = new Mock<INoteService>();
            _themeServiceMock = new Mock<IThemeService>();
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

            _noteServiceMock
                .Setup(x => x.GetNoteAsync("1"))
                .ReturnsAsync(note);

            // Act
            var app = new App();
            var noteEditor = new NoteEditor
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object
            };
            app.MainPage = noteEditor;
            await noteEditor.LoadNote("1");

            // Assert
            var titleInput = noteEditor.FindByName<Editor>("titleInput");
            var contentInput = noteEditor.FindByName<Editor>("contentInput");
            
            Assert.Equal("Test Note", titleInput.Text);
            Assert.Equal("Test Content", contentInput.Text);
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

            _noteServiceMock
                .Setup(x => x.SaveNoteAsync(It.IsAny<Note>()))
                .ReturnsAsync(note);

            // Act
            var app = new App();
            var noteEditor = new NoteEditor
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object
            };
            app.MainPage = noteEditor;
            
            var titleInput = noteEditor.FindByName<Editor>("titleInput");
            var contentInput = noteEditor.FindByName<Editor>("contentInput");
            var saveButton = noteEditor.FindByName<Button>("saveButton");

            titleInput.Text = "Test Note";
            contentInput.Text = "Test Content";
            await saveButton.InvokeAsync(() => saveButton.Click());

            // Assert
            _noteServiceMock.Verify(x => x.SaveNoteAsync(It.Is<Note>(n => 
                n.Title == "Test Note" && 
                n.Content == "Test Content")), Times.Once);
        }

        [Fact]
        public void NoteEditor_ShowsEmptyEditor_WhenCreatingNewNote()
        {
            // Act
            var app = new App();
            var noteEditor = new NoteEditor
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object
            };
            app.MainPage = noteEditor;
            noteEditor.IsNewNote = true;

            // Assert
            var titleInput = noteEditor.FindByName<Editor>("titleInput");
            var contentInput = noteEditor.FindByName<Editor>("contentInput");
            
            Assert.Equal("New Note", titleInput.Text);
            Assert.Empty(contentInput.Text);
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

            _noteServiceMock
                .Setup(x => x.SaveNoteAsync(It.IsAny<Note>()))
                .ReturnsAsync(savedNote);

            // Act
            var app = new App();
            var noteEditor = new NoteEditor
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object
            };
            app.MainPage = noteEditor;
            
            var titleInput = noteEditor.FindByName<Editor>("titleInput");
            var contentInput = noteEditor.FindByName<Editor>("contentInput");
            var saveButton = noteEditor.FindByName<Button>("saveButton");

            titleInput.Text = "Test Note";
            contentInput.Text = "Test Content";
            await saveButton.InvokeAsync(() => saveButton.Click());

            // Assert
            var lastModifiedText = noteEditor.FindByName<Label>("lastModifiedText");
            Assert.Contains(savedNote.LastModified.ToString(), lastModifiedText.Text);
            Assert.Equal("Saved Note", titleInput.Text);
            Assert.Equal("Saved Content", contentInput.Text);
        }

        [Fact]
        public async Task NoteEditor_ShowsErrorMessage_WhenSaveFails()
        {
            // Arrange
            _noteServiceMock
                .Setup(x => x.SaveNoteAsync(It.IsAny<Note>()))
                .ThrowsAsync(new Exception("Save failed"));

            // Act
            var app = new App();
            var noteEditor = new NoteEditor
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object
            };
            app.MainPage = noteEditor;
            
            var titleInput = noteEditor.FindByName<Editor>("titleInput");
            var saveButton = noteEditor.FindByName<Button>("saveButton");

            titleInput.Text = "Test Note";
            await saveButton.InvokeAsync(() => saveButton.Click());

            // Assert
            var errorMessage = noteEditor.FindByName<Label>("errorMessage");
            Assert.Contains("Failed to save note", errorMessage.Text);
        }
    }
}
