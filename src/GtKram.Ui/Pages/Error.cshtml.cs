using GtKram.Ui.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[IgnoreAntiforgeryToken]
[AllowAnonymous]
public class ErrorModel : PageModel
{
    private readonly NodeGeneratorService _nodeGenerator;

    public int ErrorCode { get; set; }
    public string? ReturnUrl { get; set; }
    public bool Required2fa { get; set; }

    public ErrorModel(NodeGeneratorService nodeGenerator)
    {
        _nodeGenerator = nodeGenerator;
    }

    public void OnGet(int statusCode, string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl))
        {
            var node = _nodeGenerator.Find(returnUrl);
            Required2fa = node != null && node.AllowedPolicy == Policies.TwoFactorAuth;
        }

        ErrorCode = statusCode < 1 ? 500 : statusCode;
        ReturnUrl = returnUrl;
    }
}
