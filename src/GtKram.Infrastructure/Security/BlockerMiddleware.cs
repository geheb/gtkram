namespace GtKram.Infrastructure.Security;

using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public sealed class BlockerMiddleware
{
    private readonly RequestDelegate _next;

    public BlockerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null 
            || System.Net.IPAddress.IsLoopback(address) 
            || context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        var blacklist = context.RequestServices.GetRequiredService<BlacklistCache>();
        string? userAgent = context.Request.Headers.UserAgent;
        var trackedItem = blacklist.Get(address, userAgent);
        if (trackedItem?.IsSuspicious == true)
        {
            await CreateBannedResponse(context);
            return;
        }

        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status404NotFound &&
            !context.Request.Path.StartsWithSegments("/Error"))
        {
            if (trackedItem is not null)
            {
                blacklist.Update(address, userAgent, false);
            }
            else
            {
                var checker = context.RequestServices.GetRequiredService<IpReputationChecker>();
                var isListed = await checker.IsListed(address);
                blacklist.Update(address, userAgent, isListed);
            }
        }
    }

    private static async Task CreateBannedResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status418ImATeapot;
        context.Response.Headers["Connection"] = "close";
        await context.Response.WriteAsync("You are banned on this site!", context.RequestAborted);

        var shouldAbortConnection = new Random().Next() % 2 == 0;
        if (shouldAbortConnection)
        {
            var connection = context.Features.Get<IConnectionLifetimeFeature>();
            if (connection is not null)
            {
                connection.Abort();
            }
            else
            {
                context.Abort();
            }
        }
    }
}
