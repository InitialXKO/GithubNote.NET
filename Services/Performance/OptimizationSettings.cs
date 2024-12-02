using System;
using System.Collections.Generic;

namespace GithubNote.NET.Services.Performance
{
    public class OptimizationSettings
    {
        public int MaxCacheSize { get; set; }
        public TimeSpan DefaultCacheExpiration { get; set; }
        public bool EnablePreloading { get; set; }
        public int PreloadBatchSize { get; set; }
        public Dictionary<string, bool> ContextPreloadSettings { get; set; }
    }
}
