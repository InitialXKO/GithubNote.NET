using GithubNote.NET.Models;
using GithubNote.NET.UI;
using GithubNote.NET.UI.ViewModels;
using Moq;
using System.Collections.ObjectModel;
using Xunit;

namespace GithubNote.NET.Tests.ViewModels
{
    public class NoteListViewModelTests : ViewModelTestBase
    {
        private readonly NoteListViewModel _viewModel;

        public NoteListViewModelTests()
        {
            _viewModel = new NoteListViewModel(
                MockNoteService.Object,
                MockAuthService.Object,
                MockNavigationService.Object);
        }

        [Fact]
        public async Task LoadNotes_Success_PopulatesNotesList()
        {
            // Arrange
            var testNotes = new List<Note>
            {
                new Note { Id = "1", Title = "Test Note 1" },
                new Note { Id = "2", Title = "Test Note 2" }
            };
            MockNoteService.Setup(x => x.GetNotesAsync())
                .ReturnsAsync(testNotes);

            // Act
            await _viewModel.LoadNotesAsync();

            // Assert
            Assert.Equal(2, _viewModel.Notes.Count);
            Assert.Equal("Test Note 1", _viewModel.Notes[0].Title);
            Assert.Equal("Test Note 2", _viewModel.Notes[1].Title);
            VerifyNoErrors(_viewModel.ErrorMessage);
        }

        [Fact]
        public async Task LoadNotes_Error_ShowsErrorMessage()
        {
            // Arrange
            MockNoteService.Setup(x => x.GetNotesAsync())
                .ThrowsAsync(new Exception("Test error"));

            // Act
            await _viewModel.LoadNotesAsync();

            // Assert
            Assert.Empty(_viewModel.Notes);
            VerifyError(_viewModel.ErrorMessage, "Test error");
        }

        [Fact]
        public async Task CreateNote_NavigatesToEditor()
        {
            // Act
            await _viewModel.CreateNoteCommand.ExecuteAsync(null);

            // Assert
            MockNavigationService.Verify(
                x => x.NavigateToAsync(Routes.NoteEditor),
                Times.Once);
        }

        [Fact]
        public async Task SearchNotes_FiltersByQuery()
        {
            // Arrange
            var testNotes = new List<Note>
            {
                new Note { Id = "1", Title = "Test Note 1" },
                new Note { Id = "2", Title = "Different Note" }
            };
            MockNoteService.Setup(x => x.GetNotesAsync())
                .ReturnsAsync(testNotes);
            await _viewModel.LoadNotesAsync();

            // Act
            _viewModel.SearchQuery = "Test";
            await _viewModel.SearchCommand.ExecuteAsync(null);

            // Assert
            Assert.Single(_viewModel.Notes);
            Assert.Equal("Test Note 1", _viewModel.Notes[0].Title);
        }

        [Fact]
        public async Task DeleteNote_RemovesFromList()
        {
            // Arrange
            var noteToDelete = new Note { Id = "1", Title = "Test Note" };
            _viewModel.Notes.Add(noteToDelete);
            MockNoteService.Setup(x => x.DeleteNoteAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _viewModel.DeleteNoteCommand.ExecuteAsync(noteToDelete);

            // Assert
            Assert.Empty(_viewModel.Notes);
            MockNoteService.Verify(
                x => x.DeleteNoteAsync(noteToDelete.Id),
                Times.Once);
        }
    }
}
