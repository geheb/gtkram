namespace GtKram.Infrastructure.Security;

using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

public sealed class SecurityHeadersMiddleware
{
    internal const string NonceKey = "CspNonce";

    private static readonly string _cspBeforeNonce;
    private static readonly string _cspAfterNonce;
    private readonly RequestDelegate _next;

    static SecurityHeadersMiddleware()
    {
        var before = new StringBuilder();
        before.Append(GetDirective("default-src", "'self'"));
        before.Append("script-src 'self' 'nonce-");

        var after = new StringBuilder();
        after.Append("'; ");
        after.Append(GetDirective("style-src", "'self'"));
        after.Append(GetDirective("img-src", "'self'", "data:"));
        after.Append(GetDirective("font-src", "'self'"));
        after.Append(GetDirective("media-src", "'self'"));
        after.Append(GetDirective("connect-src", "'self'"));
        after.Append(GetDirective("worker-src", "'self'"));
        after.Append(GetDirective("frame-ancestors", "'none'"));
        after.Append(GetDirective("object-src", "'none'"));
        after.Append(GetDirective("base-uri", "'self'"));
        after.Append(GetDirective("form-action", "'self'"));

        _cspBeforeNonce = before.ToString();
        _cspAfterNonce = after.ToString();
    }

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[NonceKey] = nonce;

        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = string.Concat(_cspBeforeNonce, nonce, _cspAfterNonce);
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=self, microphone=(), geolocation=()";
        await _next(context);
    }

    private static string GetDirective(string directive, params string[] sources)
        => $"{directive} {string.Join(" ", sources)}; ";
}
