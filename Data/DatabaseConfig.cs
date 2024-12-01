using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GithubNote.NET.Data
{
    public static class DatabaseConfig
    {
        public static IServiceCollection AddGithubNoteDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 注册DbContext
            services.AddDbContext<GithubNoteDbContext>(options =>
            {
                // 根据配置选择数据库提供程序
                var provider = configuration.GetValue<string>("Database:Provider")?.ToLower() ?? "sqlite";
                var connectionString = configuration.GetConnectionString("DefaultConnection");

                switch (provider)
                {
                    case "sqlite":
                        options.UseSqlite(connectionString);
                        break;
                    case "sqlserver":
                        options.UseSqlServer(connectionString);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported database provider: {provider}");
                }

                // 在开发环境启用详细日志
                if (configuration.GetValue<bool>("Database:EnableDetailedLogs"))
                {
                    options.EnableSensitiveDataLogging()
                           .EnableDetailedErrors();
                }
            });

            // 注册仓储
            services.AddScoped<INoteRepository, NoteRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }

        public static IApplicationBuilder UseGithubNoteDatabase(
            this IApplicationBuilder app,
            IConfiguration configuration)
        {
            // 自动执行数据库迁移
            if (configuration.GetValue<bool>("Database:AutoMigrate"))
            {
                using var scope = app.ApplicationServices.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GithubNoteDbContext>();
                context.Database.Migrate();
            }

            return app;
        }
    }
}
