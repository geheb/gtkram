using GtKram.WebApp.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[AllowAnonymous]
public class ErrorModel : PageModel
{
    private readonly NodeGeneratorService _nodeGeneratorService;

    public int Code { get; set; }
    public string? Description { get; set; }
    public bool Is2faRequired { get; set; }

    public ErrorModel(NodeGeneratorService nodeGeneratorService)
    {
        _nodeGeneratorService = nodeGeneratorService;
    }

    public void OnGet(int code, string? returnUrl = null)
        => HandleError(code, returnUrl);

    public void OnPost(int code)
        => HandleError(code);

    private void HandleError(int code, string? returnUrl = null)
    {
        Code = code < 1 ? 500 : code;
        Description = code switch
        {
            400 => "Die Anfrage ist ungültig.",
            403 => $"Der Zugriff auf die angeforderte Seite '{returnUrl}' wurde verweigert.",
            404 => "Die angeforderte Seite wurde nicht gefunden.",
            _ => "Ein interner Server-Fehler ist aufgetreten."
        };

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            var node = _nodeGeneratorService.Find(returnUrl);
            Is2faRequired = node?.AllowedPolicy == Policies.TwoFactorAuth;
        }
    }
}
