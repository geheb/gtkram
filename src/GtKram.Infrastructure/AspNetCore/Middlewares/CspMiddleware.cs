namespace GtKram.Infrastructure.AspNetCore.Middlewares;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text;

public sealed class CspMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly StringValues _headerValues;

    static CspMiddleware()
    {
        _headerValues = GetHeaderValues();
    }

    public CspMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.Headers.ContentSecurityPolicy = _headerValues;

        await _next(context);
    }

    private static string GetHeaderValues()
    {
        var value = new StringBuilder();
        value.Append(GetDirective("default-src", "'self'"));
        value.Append(GetDirective("script-src", "'self'", "'unsafe-inline'"));
        value.Append(GetDirective("style-src", "'self'", "'unsafe-inline'"));
        value.Append(GetDirective("img-src", "'self'", "data:"));
        value.Append(GetDirective("font-src", "'self'"));
        value.Append(GetDirective("media-src", "'self'"));
        return value.ToString();
    }

    private static string GetDirective(string directive, params string[] sources)
        => $"{directive} {string.Join(" ", sources)}; ";
}
