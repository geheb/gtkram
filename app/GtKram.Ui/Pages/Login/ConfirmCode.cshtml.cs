using GtKram.Application.UseCases.User.Commands;
using GtKram.Ui.Annotations;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class ConfirmCodeModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty, Display(Name = "Bestätigungscode aus der App")]
    [RequiredField, TextLengthField(6, MinimumLength = 6)]
    public string? Code { get; set; }

    [BindProperty, Display(Name = "Diesen Browser vertrauen")]
    public bool IsTrustBrowser { get; set; }

    public string? ReturnUrl { get; set; }

    public bool IsDisabled { get; set; }

    public ConfirmCodeModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (User.Identity?.IsAuthenticated ?? false)
        {
            return LocalRedirect("/");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SignInOtpCommand(Code!, IsTrustBrowser), cancellationToken);

        if (result.IsSuccess)
        {
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
        }

        ModelState.AddError(result.Errors);
        return Page();
    }
}
