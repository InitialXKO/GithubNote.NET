using GithubNote.NET.Services;
using Moq;
using Xunit;

namespace GithubNote.NET.Tests.ViewModels
{
    public abstract class ViewModelTestBase
    {
        protected readonly Mock<INoteService> MockNoteService;
        protected readonly Mock<INavigationService> MockNavigationService;
        protected readonly Mock<IAuthenticationService> MockAuthService;
        protected readonly Mock<INoteSync> MockNoteSync;

        protected ViewModelTestBase()
        {
            MockNoteService = new Mock<INoteService>();
            MockNavigationService = new Mock<INavigationService>();
            MockAuthService = new Mock<IAuthenticationService>();
            MockNoteSync = new Mock<INoteSync>();
        }

        protected void VerifyNoErrors(string errorMessage)
        {
            Assert.True(string.IsNullOrEmpty(errorMessage), $"Unexpected error: {errorMessage}");
        }

        protected void VerifyError(string errorMessage, string expectedError)
        {
            Assert.False(string.IsNullOrEmpty(errorMessage), "Expected error message but got none");
            Assert.Contains(expectedError, errorMessage);
        }
    }
}
