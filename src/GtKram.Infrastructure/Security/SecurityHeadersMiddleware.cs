namespace GtKram.Infrastructure.Security;

using Microsoft.AspNetCore.Http;
using System.Text;

public sealed class SecurityHeadersMiddleware
{
    private static readonly string _cspHeaderValues;
    private readonly RequestDelegate _next;

    static SecurityHeadersMiddleware()
    {
        _cspHeaderValues = GetCspHeaderValues();
    }

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = _cspHeaderValues;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=self, microphone=(), geolocation=()";
        await _next(context);
    }

    private static string GetCspHeaderValues()
    {
        var value = new StringBuilder();
        value.Append(GetDirective("default-src", "'self'"));
        value.Append(GetDirective("script-src", "'self'", "'unsafe-inline'"));
        value.Append(GetDirective("style-src", "'self'", "'unsafe-inline'"));
        value.Append(GetDirective("img-src", "'self'", "data:"));
        value.Append(GetDirective("font-src", "'self'"));
        value.Append(GetDirective("media-src", "'self'"));
        value.Append(GetDirective("connect-src", "'self'"));
        value.Append(GetDirective("worker-src", "'self'"));
        value.Append(GetDirective("frame-ancestors", "'none'"));
        return value.ToString();
    }

    private static string GetDirective(string directive, params string[] sources)
        => $"{directive} {string.Join(" ", sources)}; ";
}
