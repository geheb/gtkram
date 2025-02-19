using GtKram.Application.UseCases.User.Commands;
using GtKram.Ui.Converter;
using GtKram.Ui.I18n;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.Login;

[AllowAnonymous]
public class ConfirmChangeEmailModel : PageModel
{
    private readonly IMediator _mediator;

    public string ConfirmedEmail { get; set; } = "n.v.";

    public ConfirmChangeEmailModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid id, string email, string token, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidRequest);
            return;
        }

        ConfirmedEmail = new EmailConverter().Anonymize(email);

        var result = await _mediator.Send(new ConfirmChangeEmailCommand(id, email, token), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, LocalizedMessages.InvalidNewEmailConfirmationLink);
            return;
        }       
    }
}
