using Microsoft.Extensions.Configuration;

namespace GithubNote.NET.Services
{
    public class AppConfig
    {
        public DatabaseConfig Database { get; set; }
        public GitHubConfig GitHub { get; set; }
        public CacheConfig Cache { get; set; }
        public LoggingConfig Logging { get; set; }

        public class DatabaseConfig
        {
            public string Provider { get; set; } = "SQLite";
            public string ConnectionString { get; set; }
            public bool EnableSensitiveDataLogging { get; set; }
            public int CommandTimeout { get; set; } = 30;
        }

        public class GitHubConfig
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string RedirectUri { get; set; }
            public string[] Scopes { get; set; }
            public int TokenExpiryDays { get; set; } = 7;
        }

        public class CacheConfig
        {
            public string Type { get; set; } = "Memory";
            public int DefaultExpirationMinutes { get; set; } = 30;
            public string RedisConnection { get; set; }
            public long? MemoryCacheSizeLimit { get; set; }
        }

        public class LoggingConfig
        {
            public string LogLevel { get; set; } = "Information";
            public bool EnableConsoleLogging { get; set; } = true;
            public bool EnableFileLogging { get; set; } = true;
            public string LogFilePath { get; set; }
            public int RetainedFileCountLimit { get; set; } = 31;
        }

        public static AppConfig LoadFromConfiguration(IConfiguration configuration)
        {
            var config = new AppConfig();
            configuration.Bind(config);
            return config;
        }
    }
}
