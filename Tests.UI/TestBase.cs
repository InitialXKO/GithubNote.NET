using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using GithubNote.NET.Services;
using GithubNote.NET.Services.UI;
using GithubNote.NET.Services.Performance.Interfaces;
using GithubNote.NET.Services.UI.ErrorHandling;
using GithubNote.NET.Cache;
using Microsoft.Extensions.Logging;

namespace GithubNote.NET.Tests.UI
{
    public abstract class TestBase : TestContext
    {
        protected Mock<INoteService> MockNoteService;
        protected Mock<IUIStateManager> MockStateManager;
        protected Mock<ILogger<NoteUIService>> MockLogger;
        protected Mock<IPerformanceMonitor> MockPerformanceMonitor;
        protected Mock<IErrorHandlingService> MockErrorHandling;
        protected Mock<IPerformanceOptimizer> MockOptimizer;
        protected Mock<OptimizedNoteCache> MockNoteCache;
        protected INoteUIService NoteUIService;

        protected TestBase()
        {
            MockNoteService = new Mock<INoteService>();
            MockStateManager = new Mock<IUIStateManager>();
            MockLogger = new Mock<ILogger<NoteUIService>>();
            MockPerformanceMonitor = new Mock<IPerformanceMonitor>();
            MockErrorHandling = new Mock<IErrorHandlingService>();
            MockOptimizer = new Mock<IPerformanceOptimizer>();
            MockNoteCache = new Mock<OptimizedNoteCache>();

            NoteUIService = new NoteUIService(
                MockNoteService.Object,
                MockStateManager.Object,
                MockLogger.Object,
                MockPerformanceMonitor.Object,
                MockErrorHandling.Object,
                MockOptimizer.Object,
                MockNoteCache.Object);

            Services.AddSingleton(MockNoteService.Object);
            Services.AddSingleton(MockStateManager.Object);
            Services.AddSingleton(MockLogger.Object);
            Services.AddSingleton(MockPerformanceMonitor.Object);
            Services.AddSingleton(MockErrorHandling.Object);
            Services.AddSingleton(MockOptimizer.Object);
            Services.AddSingleton(MockNoteCache.Object);
            Services.AddSingleton(NoteUIService);
        }

        protected void SetupDefaultMocks()
        {
            MockStateManager
                .Setup(x => x.SetStateAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            MockPerformanceMonitor
                .Setup(x => x.TrackOperationAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
        }
    }
}
