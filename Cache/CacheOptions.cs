using System;

namespace GithubNote.NET.Cache
{
    public class CacheOptions
    {
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
        public int MaxItems { get; set; } = 1000;
        public bool EnableCompression { get; set; } = true;
    }
}
