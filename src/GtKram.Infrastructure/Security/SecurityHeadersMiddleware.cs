namespace GtKram.Infrastructure.Security;

using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

public sealed class SecurityHeadersMiddleware
{
    internal const string NonceKey = "CspNonce";

    private static readonly string _cspTemplate;
    private static readonly string _cspNonce;
    private readonly RequestDelegate _next;

    static SecurityHeadersMiddleware()
    {
        _cspNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

        var directives = new StringBuilder();
        directives.Append(GetDirective("default-src", "'self'"));
        directives.Append(GetDirective("script-src", "'self'", $"'nonce-{_cspNonce}'"));
        directives.Append(GetDirective("style-src", "'self'", "'unsafe-inline'"));
        directives.Append(GetDirective("img-src", "'self'", "data:"));
        directives.Append(GetDirective("font-src", "'self'"));
        directives.Append(GetDirective("media-src", "'self'"));
        directives.Append(GetDirective("connect-src", "'self'"));
        directives.Append(GetDirective("worker-src", "'self'"));
        directives.Append(GetDirective("frame-ancestors", "'none'"));
        _cspTemplate = directives.ToString();
    }

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Items[NonceKey] = _cspNonce;
        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = _cspTemplate;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=self, microphone=(), geolocation=()";
        await _next(context);
    }

    private static string GetDirective(string directive, params string[] sources)
        => $"{directive} {string.Join(" ", sources)}; ";
}
