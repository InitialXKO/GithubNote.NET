using GithubNote.NET.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GithubNote.NET.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(GithubNoteDbContext context)
        {
            // 确保数据库已创建
            await context.Database.EnsureCreatedAsync();

            // 检查是否已有数据
            if (await context.Users.AnyAsync())
            {
                return; // 数据库已经初始化
            }

            // 创建默认用户
            var defaultUser = new User
            {
                Username = "admin",
                Email = "admin@example.com",
                Settings = new UserSettings
                {
                    DarkMode = false,
                    DefaultEditor = "markdown",
                    AutoSync = true,
                    AutoSaveInterval = 60,
                    Language = "en-US",
                    Theme = "light"
                },
                LastLoginTime = DateTime.UtcNow
            };

            await context.Users.AddAsync(defaultUser);

            // 创建示例笔记
            var sampleNote = new Note
            {
                Title = "Welcome to GithubNote",
                Content = "# Welcome to GithubNote\n\nThis is a sample note to help you get started.",
                IsPrivate = false,
                Tags = new List<string> { "welcome", "tutorial" },
                Metadata = new NoteMetadata
                {
                    Version = "1.0",
                    Format = "markdown",
                    CustomProperties = new Dictionary<string, string>
                    {
                        { "category", "tutorial" }
                    }
                }
            };

            await context.Notes.AddAsync(sampleNote);

            // 添加示例评论
            var sampleComment = new Comment
            {
                Content = "Welcome to GithubNote! Feel free to explore all features.",
                AuthorId = defaultUser.Id,
                AuthorName = defaultUser.Username,
                NoteId = sampleNote.Id
            };

            await context.Comments.AddAsync(sampleComment);

            // 保存更改
            await context.SaveChangesAsync();
        }
    }
}
