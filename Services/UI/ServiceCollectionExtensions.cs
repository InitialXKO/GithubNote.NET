using Microsoft.Extensions.DependencyInjection;
using GithubNote.NET.Services.UI.ErrorHandling;
using GithubNote.NET.Services.Performance;
using GithubNote.NET.Cache;

namespace GithubNote.NET.Services.UI
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            services.AddSingleton<IUIStateManager, UIStateManager>();
            services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
            services.AddSingleton<IPerformanceOptimizer, PerformanceOptimizer>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<OptimizedNoteCache>();
            services.AddScoped<INoteUIService, NoteUIService>();

            services.Configure<PerformanceThresholds>(options =>
            {
                options.MaxResponseTime = TimeSpan.FromSeconds(2);
                options.MaxMemoryUsage = 512 * 1024 * 1024; // 512MB
                options.MaxActiveConnections = 100;
                options.MinCacheHitRate = 70;
            });
            
            return services;
        }
    }
}
