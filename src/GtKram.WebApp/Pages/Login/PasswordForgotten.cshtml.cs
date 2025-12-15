using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.WebApp.Annotations;
using GtKram.WebApp.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Pages.Login;

[AllowAnonymous]
public class PasswordForgottenModel : PageModel
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    [BindProperty]
    public string? UserName { get; set; } // just for Bots

    [BindProperty, Display(Name = "E-Mail-Adresse")]
    [RequiredField, EmailLengthField, EmailField]
    public string? Email { get; set; }

    [BindProperty]
    public bool IsDisabled { get; set; }

    public PasswordForgottenModel(
        ILogger<PasswordForgottenModel> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(UserName)) 
        {
            IsDisabled = true;
            _logger.LogWarning("Ungültige Anfrage von {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddError(Domain.Errors.Internal.InvalidRequest);
            return Page();
        }

        if (!ModelState.IsValid) return Page();

        var callbackUrl = Url.PageLink("/Login/ConfirmResetPassword", values: new { id = User.GetId(), token = string.Empty });

        await _mediator.Send(new SendResetPasswordCommand(Email!, callbackUrl!), cancellationToken);

        return RedirectToPage("Index", new { message = 2 });
    }
}
