using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using GithubNote.NET.Data;
using GithubNote.NET.Repositories;
using GithubNote.NET.Cache;
using GithubNote.NET.Authentication;
using GithubNote.NET.Services.Performance;
using GithubNote.NET.Services.Performance.Interfaces;
using GithubNote.NET.Services.Performance.Implementation;
using GithubNote.NET.Services.UI;
using GithubNote.NET.Services.UI.Theme;
using GithubNote.NET.UI.Controls;
using GithubNote.NET.UI.ViewModels;
using Microsoft.Extensions.Logging;
using Serilog;
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
            services.AddDbContext<GithubNoteDbContext>((serviceProvider, options) =>
            {
                var dbConfig = appConfig.Database;
                switch (dbConfig.Provider.ToLower())
                {
                    case "sqlite":
                        options.UseSqlite(dbConfig.ConnectionString);
                        break;
                    case "sqlserver":
                        options.UseSqlServer(dbConfig.ConnectionString, sqlServerOptions =>
                        {
                            sqlServerOptions.CommandTimeout(dbConfig.CommandTimeout);
                        });
                        break;
                    default:
                        throw new ArgumentException($"Unsupported database provider: {dbConfig.Provider}");
                }

                options.EnableSensitiveDataLogging(dbConfig.EnableSensitiveDataLogging);

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
            services.AddSingleton<GithubNote.NET.Services.Performance.Interfaces.IPerformanceMonitor>(sp => sp.GetRequiredService<PerformanceMonitor>());
            services.AddSingleton<IPerformanceOptimizer, PerformanceOptimizer>();
            services.AddSingleton<IThemeService, ThemeService>();

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
            services.AddSingleton<GithubNote.NET.Services.UI.IActivityIndicator, GithubNote.NET.Services.UI.LoadingIndicator>();
            services.AddSingleton<IUIService, UIService>();
            services.AddSingleton<IStateService, StateService>();
            services.AddSingleton(Preferences.Default);

            // 添加视图模型
            services.AddTransient<NoteListViewModel>();
            services.AddTransient<NoteEditorViewModel>();

            // 配置HttpClient
            services.AddHttpClient<NoteSyncService>();

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // 添加日志服务
            services.AddLogging(builder =>
            {
                var loggingConfig = appConfig.Logging;
                builder.SetMinimumLevel(Enum.Parse<LogLevel>(loggingConfig.LogLevel));

                if (loggingConfig.EnableConsoleLogging)
                {
                    builder.AddConsole();
                }

                builder.AddSerilog();
            });

            return services;
        }
    }
}
