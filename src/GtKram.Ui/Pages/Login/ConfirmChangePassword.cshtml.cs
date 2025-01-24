using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Ui.Annotations;
using GtKram.Ui.Converter;
using GtKram.Ui.I18n;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class ConfirmChangePasswordModel : PageModel
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    [BindProperty]
    public string? UserName { get; set; } // just for bots

    [BindProperty, Display(Name = "Neues Passwort")]
    [RequiredField, PasswordLengthField]
    public string? Password { get; set; }

    [BindProperty, Display(Name = "Neues Passwort wiederholen")]
    [RequiredField, PasswordLengthField]
    [CompareField(nameof(Password))]
    public string? RepeatPassword { get; set; }

    public bool IsDisabled { get; set; }

    public string ChangePasswordEmail { get; set; } = "n.v.";

    public ConfirmChangePasswordModel(
        ILogger<ConfirmChangePasswordModel> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, string token, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(token))
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return;
        }

        var result = await _mediator.Send(new VerifyConfirmChangePasswordQuery(id, token), cancellationToken);
        if (result.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidPasswordResetLink);
            return;
        }

        var resultUser = await _mediator.Send(new FindUserByIdQuery(id), cancellationToken);
        if (resultUser.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return;
        }

        ChangePasswordEmail = new EmailConverter().Anonymize(resultUser.Value.Email);
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string token, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(token) || !string.IsNullOrWhiteSpace(UserName))
        {
            IsDisabled = true;
            _logger.LogWarning("Bad request from {Ip}", HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return Page();
        }

        var resultUser = await _mediator.Send(new FindUserByIdQuery(id), cancellationToken);
        if (resultUser.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return Page();
        }

        ChangePasswordEmail = new EmailConverter().Anonymize(resultUser.Value.Email);

        if (!ModelState.IsValid) return Page();

        var result = await _mediator.Send(new ConfirmChangePasswordCommand(id, Password!, token), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidPasswordResetLink);
            return Page();
        }

        return RedirectToPage("Index", new { message = 1 });
    }
}
