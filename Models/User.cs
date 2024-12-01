using System;

namespace GithubNote.NET.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public string GithubToken { get; set; }
        public UserSettings Settings { get; set; }
        public DateTime LastLoginTime { get; set; }

        public User()
        {
            Id = Guid.NewGuid().ToString();
            Settings = new UserSettings();
        }
    }

    public class UserSettings
    {
        public bool DarkMode { get; set; }
        public string DefaultEditor { get; set; }
        public bool AutoSync { get; set; }
        public int AutoSaveInterval { get; set; }
        public string Language { get; set; }
        public string Theme { get; set; }

        public UserSettings()
        {
            DefaultEditor = "markdown";
            AutoSaveInterval = 60;
            Language = "en-US";
            Theme = "light";
        }
    }
}
