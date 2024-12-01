using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using GithubNote.NET.Services;
using GithubNote.NET.Services.UI;
using Microsoft.Extensions.Logging;

namespace GithubNote.NET.Tests.UI
{
    public abstract class TestBase : TestContext
    {
        protected Mock<INoteService> MockNoteService;
        protected Mock<IUIStateManager> MockStateManager;
        protected Mock<ILogger<NoteUIService>> MockLogger;
        protected Mock<IPerformanceMonitor> MockPerformanceMonitor;
        protected INoteUIService NoteUIService;

        protected TestBase()
        {
            MockNoteService = new Mock<INoteService>();
            MockStateManager = new Mock<IUIStateManager>();
            MockLogger = new Mock<ILogger<NoteUIService>>();
            MockPerformanceMonitor = new Mock<IPerformanceMonitor>();

            NoteUIService = new NoteUIService(
                MockNoteService.Object,
                MockStateManager.Object,
                MockLogger.Object,
                MockPerformanceMonitor.Object);

            Services.AddSingleton(MockNoteService.Object);
            Services.AddSingleton(MockStateManager.Object);
            Services.AddSingleton(MockLogger.Object);
            Services.AddSingleton(MockPerformanceMonitor.Object);
            Services.AddSingleton(NoteUIService);
        }

        protected void SetupDefaultMocks()
        {
            MockStateManager
                .Setup(x => x.SetStateAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            MockPerformanceMonitor
                .Setup(x => x.TrackOperation(It.IsAny<string>(), It.IsAny<TimeSpan>()));
        }
    }
}
