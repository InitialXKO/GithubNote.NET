namespace GithubNote.NET.Services.Performance
{
    public class CacheStatistics
    {
        public int TotalRequests { get; set; }
        public int Hits { get; set; }
        public int Misses { get; set; }
        public int HitRate => TotalRequests == 0 ? 0 : (Hits * 100) / TotalRequests;
        public long MemoryUsage { get; set; }
        public int ItemCount { get; set; }
        public long TotalCacheSize { get; set; }
        public double CacheHitRate { get; set; }
        public int CacheEntries { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
