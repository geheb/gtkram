namespace GtKram.Infrastructure.AspNetCore.Middlewares;

using GtKram.Infrastructure.Email;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

public sealed class BotBlockerMiddleware
{
    private readonly RequestDelegate _next;

    public BotBlockerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null || IPAddress.IsLoopback(address))
        {
            await _next(context);
            return;
        }

        var key = "bot-" + address;

        var memoryCache = context.RequestServices.GetRequiredService<IMemoryCache>();
        if (memoryCache.TryGetValue(key, out int notFoundCounter) && notFoundCounter >= 7)
        {
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;
            context.Response.Headers["Connection"] = "close";
            if (notFoundCounter == 7)
            {
                await context.Response.WriteAsync("You are banned on this site!", context.RequestAborted);
                memoryCache.Set(key, notFoundCounter + 1, DateTimeOffset.UtcNow.AddHours(1));
            }
            else
            {
                var connection = context.Features.Get<IConnectionLifetimeFeature>();
                if (connection is null)
                {
                    context.Abort();
                }
                else
                {
                    connection.Abort();
                }
            }
            return;
        }

        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            var expirationMinutes = new Random().Next(60, 180);

            var reputationChecker = context.RequestServices.GetRequiredService<IpReputationChecker>();
            if (await reputationChecker.IsListed(address))
            {
                memoryCache.Set(key, int.MaxValue, DateTimeOffset.UtcNow.AddMinutes(expirationMinutes));
            }
            else
            {
                memoryCache.Set(key, ++notFoundCounter, DateTimeOffset.UtcNow.AddHours(1));
            }
        }
    }
}
