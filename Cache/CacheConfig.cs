using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

namespace GithubNote.NET.Cache
{
    public static class CacheConfig
    {
        public static IServiceCollection AddGithubNoteCache(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var cacheType = configuration.GetValue<string>("Cache:Type")?.ToLower() ?? "memory";

            switch (cacheType)
            {
                case "memory":
                    services.AddMemoryCache(options =>
                    {
                        options.SizeLimit = configuration.GetValue<long?>("Cache:Memory:SizeLimit");
                        options.ExpirationScanFrequency = TimeSpan.FromMinutes(
                            configuration.GetValue<int>("Cache:Memory:ExpirationScanFrequencyMinutes", 5));
                    });
                    services.AddScoped<ICacheService, MemoryCacheService>();
                    break;

                case "redis":
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = configuration.GetConnectionString("Redis");
                        options.InstanceName = "GithubNote_";
                    });
                    services.AddScoped<ICacheService, DistributedCacheService>();
                    break;

                default:
                    throw new ArgumentException($"Unsupported cache type: {cacheType}");
            }

            return services;
        }
    }
}
