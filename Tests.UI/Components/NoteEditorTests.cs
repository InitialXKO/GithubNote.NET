using Microsoft.Maui.Controls;
using Moq;
using Xunit;
using GithubNote.NET.Services;
using GithubNote.NET.UI.Controls;

namespace GithubNote.NET.Tests.UI.Components
{
    public class NoteEditorTests
    {
        private readonly Mock<INoteService> _noteServiceMock;
        private readonly Mock<IThemeService> _themeServiceMock;
        private readonly IDispatcher _dispatcher;

        public NoteEditorTests()
        {
            _noteServiceMock = new Mock<INoteService>();
            _themeServiceMock = new Mock<IThemeService>();
            _dispatcher = Application.Current?.Dispatcher ?? new DispatcherMock();
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
            var noteEditor = new NoteEditor(_dispatcher)
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object
            };

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
            var noteEditor = new NoteEditor(_dispatcher)
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object
            };
            
            var titleInput = noteEditor.FindByName<Editor>("titleInput");
            var contentInput = noteEditor.FindByName<Editor>("contentInput");
            var saveButton = noteEditor.FindByName<Button>("saveButton");

            titleInput.Text = "Test Note";
            contentInput.Text = "Test Content";
            await saveButton.Command.ExecuteAsync(null);

            // Assert
            _noteServiceMock.Verify(x => x.SaveNoteAsync(It.Is<Note>(n => 
                n.Title == "Test Note" && 
                n.Content == "Test Content")), Times.Once);
        }

        [Fact]
        public void NoteEditor_ShowsEmptyEditor_WhenCreatingNewNote()
        {
            // Act
            var noteEditor = new NoteEditor(_dispatcher)
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object,
                IsNewNote = true
            };

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
            var noteEditor = new NoteEditor(_dispatcher)
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object
            };
            
            var titleInput = noteEditor.FindByName<Editor>("titleInput");
            var contentInput = noteEditor.FindByName<Editor>("contentInput");
            var saveButton = noteEditor.FindByName<Button>("saveButton");

            titleInput.Text = "Test Note";
            contentInput.Text = "Test Content";
            await saveButton.Command.ExecuteAsync(null);

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
            var noteEditor = new NoteEditor(_dispatcher)
            {
                NoteService = _noteServiceMock.Object,
                ThemeService = _themeServiceMock.Object
            };
            
            var titleInput = noteEditor.FindByName<Editor>("titleInput");
            var saveButton = noteEditor.FindByName<Button>("saveButton");

            titleInput.Text = "Test Note";
            await saveButton.Command.ExecuteAsync(null);

            // Assert
            var errorMessage = noteEditor.FindByName<Label>("errorMessage");
            Assert.Contains("Failed to save note", errorMessage.Text);
        }
    }

    // Mock dispatcher for testing
    internal class DispatcherMock : IDispatcher
    {
        public bool IsDispatchRequired => false;

        public IDispatcherTimer CreateTimer() => throw new NotImplementedException();

        public bool Dispatch(Action action)
        {
            action();
            return true;
        }

        public bool DispatchDelayed(TimeSpan delay, Action action) => true;
    }
}
