namespace GtKram.Infrastructure.AspNetCore.Middlewares;

using System.Net;
using GtKram.Infrastructure.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

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
            await context.Response.WriteAsync("(”’\\(*o*)/”’) You are banned on this site!", context.RequestAborted);
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
