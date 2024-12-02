using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;

namespace GithubNote.NET.Authentication
{
    public static class AuthenticationConfig
    {
        public static IServiceCollection AddGithubNoteAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 验证配置
            var clientId = configuration["GitHub:ClientId"];
            var clientSecret = configuration["GitHub:ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException(
                    "GitHub OAuth credentials must be configured in appsettings.json");
            }

            // 配置HttpClient
            services.AddHttpClient<IAuthenticationService, GitHubAuthenticationService>(client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "GithubNote.NET");
            });

            // 注册认证服务
            services.AddScoped<IAuthenticationService, GitHubAuthenticationService>();

            return services;
        }

        public static IApplicationBuilder UseGithubNoteAuthentication(
            this IApplicationBuilder app)
        {
            app.UseMiddleware<AuthenticationMiddleware>();
            return app;
        }
    }
}
