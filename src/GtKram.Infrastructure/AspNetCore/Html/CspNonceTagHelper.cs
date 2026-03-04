using GtKram.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace GtKram.Infrastructure.AspNetCore.Html;

[HtmlTargetElement("script")]
public sealed class CspNonceTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CspNonceTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (_httpContextAccessor.HttpContext?.Items[SecurityHeadersMiddleware.NonceKey] is string nonce)
        {
            output.Attributes.SetAttribute("nonce", nonce);
        }
    }
}
