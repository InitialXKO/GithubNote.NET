using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using GithubNote.NET.Data;
using Microsoft.EntityFrameworkCore;
using GithubNote.NET.Cache;

namespace GithubNote.NET.Services
{
    public class AppStartup
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppStartup> _logger;
        private readonly AppConfig _config;

        public AppStartup(
            IServiceProvider serviceProvider,
            ILogger<AppStartup> logger,
            AppConfig config)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _config = config;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Starting application initialization...");

                // 初始化数据库
                InitializeDatabase();

                // 初始化缓存
                await InitializeCache();

                _logger.LogInformation("Application initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application initialization");
                throw;
            }
        }

        private void InitializeDatabase()
        {
            _logger.LogInformation("Initializing database...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GithubNoteDbContext>();

            // 应用迁移
            dbContext.Database.Migrate();

            _logger.LogInformation("Database initialization completed.");
        }

        private async Task InitializeCache()
        {
            _logger.LogInformation("Initializing cache...");

            // 清理过期缓存
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            await cacheService.ClearAsync();

            _logger.LogInformation("Cache initialization completed.");
        }
    }
}
