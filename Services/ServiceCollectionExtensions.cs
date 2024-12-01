using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GithubNote.NET.Data;
using GithubNote.NET.Repositories;
using GithubNote.NET.Cache;
using GithubNote.NET.Authentication;
using Microsoft.Extensions.Logging;
using System;

namespace GithubNote.NET.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGithubNoteServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 加载应用配置
            var appConfig = AppConfig.LoadFromConfiguration(configuration);
            services.AddSingleton(appConfig);

            // 配置数据库
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var dbConfig = appConfig.Database;
                switch (dbConfig.Provider.ToLower())
                {
                    case "sqlite":
                        options.UseSqlite(dbConfig.ConnectionString);
                        break;
                    case "sqlserver":
                        options.UseSqlServer(dbConfig.ConnectionString);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported database provider: {dbConfig.Provider}");
                }

                options.EnableSensitiveDataLogging(dbConfig.EnableSensitiveDataLogging);
                options.CommandTimeout(dbConfig.CommandTimeout);

                // 配置日志
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                options.UseLoggerFactory(loggerFactory);
            });

            // 注册仓储
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<INoteRepository, NoteRepository>();

            // 添加性能监控
            services.AddSingleton<PerformanceMonitor>();
            services.AddSingleton<IPerformanceMonitor>(sp => sp.GetRequiredService<PerformanceMonitor>());

            // 添加缓存服务
            services.AddMemoryCache();
            services.Configure<CacheOptions>(configuration.GetSection("Cache"));
            services.AddSingleton<ICacheService, CacheService>();

            // 添加认证服务
            services.AddGithubNoteAuthentication(configuration);

            // 添加笔记服务
            services.AddScoped<INoteService, NoteService>();
            services.AddScoped<INoteSync, NoteSyncService>();

            // 添加导航服务
            services.AddSingleton<INavigationService, NavigationService>();

            // 添加UI服务
            services.AddSingleton<IActivityIndicator, LoadingIndicator>();
            services.AddSingleton<IUIService, UIService>();
            services.AddSingleton<IStateService, StateService>();
            services.AddSingleton(Preferences.Default);

            // 添加视图模型
            services.AddTransient<NoteListViewModel>();
            services.AddTransient<NoteEditorViewModel>();

            // 配置HttpClient
            services.AddHttpClient<NoteSyncService>();

            // 添加日志服务
            services.AddLogging(builder =>
            {
                var loggingConfig = appConfig.Logging;
                builder.SetMinimumLevel(Enum.Parse<LogLevel>(loggingConfig.LogLevel));

                if (loggingConfig.EnableConsoleLogging)
                {
                    builder.AddConsole();
                }

                if (loggingConfig.EnableFileLogging)
                {
                    builder.AddFile(loggingConfig.LogFilePath, options =>
                    {
                        options.RetainedFileCountLimit = loggingConfig.RetainedFileCountLimit;
                    });
                }
            });

            return services;
        }
    }
}
