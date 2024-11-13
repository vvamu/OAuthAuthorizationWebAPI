using Microsoft.AspNetCore.Authentication;
namespace OAuthAuthorizationWebAPI.Helpers.Middleware;
public class AddTokenToRequestMiddleware
{
    private readonly RequestDelegate _next;

    public AddTokenToRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IServiceProvider serviceProvider)
    {
        await BeforeProcessingRequest(context, serviceProvider);

        await _next(context);

        await AfterProcessingRequest(context, serviceProvider);
    }
    private async Task BeforeProcessingRequest(HttpContext context, IServiceProvider serviceProvider)
    {
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var token = await httpContextAccessor.HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
        {
            context.Request.Headers.Add("Authorization", $"Bearer {token}");
        }
    }

    private async Task AfterProcessingRequest(HttpContext context, IServiceProvider serviceProvider)
    {
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var token = await httpContextAccessor.HttpContext.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
        {
            context.Request.Headers.Add("Authorization", $"Bearer {token}");
        }
    }
}