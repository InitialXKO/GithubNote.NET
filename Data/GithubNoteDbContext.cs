using Microsoft.EntityFrameworkCore;
using GithubNote.NET.Models;
using System;

namespace GithubNote.NET.Data
{
    public class GithubNoteDbContext : DbContext
    {
        public DbSet<Note> Notes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ImageAttachment> Images { get; set; }
        public DbSet<User> Users { get; set; }

        public GithubNoteDbContext(DbContextOptions<GithubNoteDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Note配置
            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.GistId).HasMaxLength(100);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.IsPrivate).IsRequired();
                
                // 将Tags存储为JSON数组
                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

                // 配置与Comment的一对多关系
                entity.HasMany(e => e.Comments)
                    .WithOne()
                    .HasForeignKey(e => e.NoteId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 配置与ImageAttachment的一对多关系
                entity.HasMany(e => e.Images)
                    .WithOne()
                    .HasForeignKey(e => e.NoteId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 将Metadata存储为JSON对象
                entity.OwnsOne(e => e.Metadata, metadata =>
                {
                    metadata.Property(e => e.Version).IsRequired();
                    metadata.Property(e => e.Format).IsRequired();
                    metadata.Property(e => e.CustomProperties)
                        .HasConversion(
                            v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                            v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null));
                });
            });

            // Comment配置
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.AuthorId).IsRequired();
                entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // ImageAttachment配置
            modelBuilder.Entity<ImageAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Url).IsRequired();
                entity.Property(e => e.LocalPath).IsRequired();
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FileSize).IsRequired();
                entity.Property(e => e.MimeType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UploadedAt).IsRequired();
            });

            // User配置
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.Property(e => e.GithubToken).HasMaxLength(100);
                entity.Property(e => e.LastLoginTime).IsRequired();

                // 添加唯一索引
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                // 将UserSettings存储为JSON对象
                entity.OwnsOne(e => e.Settings, settings =>
                {
                    settings.Property(e => e.DefaultEditor).IsRequired();
                    settings.Property(e => e.Language).IsRequired();
                    settings.Property(e => e.Theme).IsRequired();
                });
            });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Note && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var note = (Note)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    note.CreatedAt = DateTime.UtcNow;
                }

                note.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
