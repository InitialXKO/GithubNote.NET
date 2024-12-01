using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace GithubNote.NET.Authentication
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                var authService = context.RequestServices.GetRequiredService<IAuthenticationService>();
                var token = AuthenticationHeaderValue.Parse(authHeader).Parameter;

                if (!string.IsNullOrEmpty(token) && await authService.ValidateTokenAsync(token))
                {
                    var user = await authService.GetCurrentUserAsync();
                    if (user != null)
                    {
                        context.Items["User"] = user;
                    }
                }
            }

            await _next(context);
        }
    }
}
