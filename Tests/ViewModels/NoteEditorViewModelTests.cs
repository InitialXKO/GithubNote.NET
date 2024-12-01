using GithubNote.NET.Models;
using GithubNote.NET.UI.ViewModels;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace GithubNote.NET.Tests.ViewModels
{
    public class NoteEditorViewModelTests : ViewModelTestBase
    {
        private readonly NoteEditorViewModel _viewModel;

        public NoteEditorViewModelTests()
        {
            _viewModel = new NoteEditorViewModel(
                MockNoteService.Object,
                MockNoteSync.Object,
                MockNavigationService.Object);
        }

        [Fact]
        public void LoadNote_PopulatesFields()
        {
            // Arrange
            var testNote = new Note
            {
                Id = "1",
                Title = "Test Note",
                Content = "Test Content",
                Categories = new List<string> { "Test" }
            };
            MockNoteService.Setup(x => x.GetNoteAsync("1"))
                .ReturnsAsync(testNote);

            // Act
            var query = new Dictionary<string, object> { { "noteId", "1" } };
            _viewModel.ApplyQueryAttributes(query);

            // Assert
            Assert.Equal("Test Note", _viewModel.Title);
            Assert.Equal("Test Content", _viewModel.Content);
            Assert.Single(_viewModel.Categories);
            Assert.Equal("Test", _viewModel.Categories[0]);
        }

        [Fact]
        public async Task SaveNote_Success_NavigatesBack()
        {
            // Arrange
            _viewModel.Title = "New Note";
            _viewModel.Content = "New Content";

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            MockNoteService.Verify(
                x => x.SaveNoteAsync(It.Is<Note>(n =>
                    n.Title == "New Note" &&
                    n.Content == "New Content")),
                Times.Once);
            MockNavigationService.Verify(
                x => x.GoBackAsync(),
                Times.Once);
            VerifyNoErrors(_viewModel.ErrorMessage);
        }

        [Fact]
        public async Task SaveNote_Error_ShowsErrorMessage()
        {
            // Arrange
            MockNoteService.Setup(x => x.SaveNoteAsync(It.IsAny<Note>()))
                .ThrowsAsync(new Exception("Test error"));

            // Act
            await _viewModel.SaveCommand.ExecuteAsync(null);

            // Assert
            VerifyError(_viewModel.ErrorMessage, "Failed to save note");
            MockNavigationService.Verify(
                x => x.GoBackAsync(),
                Times.Never);
        }

        [Fact]
        public async Task AddCategory_Success()
        {
            // Arrange
            string category = "NewCategory";

            // Act
            await _viewModel.AddCategoryCommand.ExecuteAsync(category);

            // Assert
            Assert.Single(_viewModel.Categories);
            Assert.Equal(category, _viewModel.Categories[0]);
        }

        [Fact]
        public async Task RemoveCategory_Success()
        {
            // Arrange
            string category = "TestCategory";
            await _viewModel.AddCategoryCommand.ExecuteAsync(category);

            // Act
            await _viewModel.RemoveCategoryCommand.ExecuteAsync(category);

            // Assert
            Assert.Empty(_viewModel.Categories);
        }

        [Fact]
        public async Task SyncNote_Success()
        {
            // Arrange
            var testNote = new Note { Id = "1", Title = "Test" };
            _viewModel.CurrentNote = testNote;
            MockNoteSync.Setup(x => x.SyncNoteAsync(testNote))
                .Returns(Task.CompletedTask);

            // Act
            await _viewModel.SyncCommand.ExecuteAsync(null);

            // Assert
            MockNoteSync.Verify(
                x => x.SyncNoteAsync(testNote),
                Times.Once);
            VerifyNoErrors(_viewModel.ErrorMessage);
        }
    }
}
