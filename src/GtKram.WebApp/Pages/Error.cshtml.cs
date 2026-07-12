using GtKram.Infrastructure.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages;

[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[AllowAnonymous]
public sealed class ErrorModel : PageModel
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
            StatusCodes.Status400BadRequest => "Deine Anfrage ist ungültig und kann nicht verarbeitet werden.",
            StatusCodes.Status403Forbidden => $"Der Zugriff auf die angeforderte Seite '{returnUrl}' wurde verweigert.",
            StatusCodes.Status404NotFound => "Die angeforderte Seite wurde nicht gefunden.",
            StatusCodes.Status408RequestTimeout => "Zeitüberschreitung bei der Verarbeitung deiner Anfrage.",
            StatusCodes.Status429TooManyRequests => "Du hast uns in letzter Zeit zu viele Anfragen gesendet. Bitte versuche es später erneut.",
            _ => "Ein interner Server-Fehler ist aufgetreten."
        };

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            var node = _nodeGeneratorService.Find(returnUrl);
            Is2faRequired = node?.AllowedPolicy == Policies.TwoFactorAuth;
        }
    }
}
