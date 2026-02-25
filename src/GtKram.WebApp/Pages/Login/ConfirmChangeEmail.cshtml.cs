using GtKram.Application.UseCases.User.Commands;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.WebApp.Converter;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Login;

[AllowAnonymous]
public sealed class ConfirmChangeEmailModel : PageModel
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
            ModelState.AddError(Domain.Errors.Internal.InvalidRequest);
            return;
        }

        ConfirmedEmail = new EmailConverter().Anonymize(email);

        var result = await _mediator.Send(new ConfirmChangeEmailCommand(id, email, token), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(Domain.Errors.Identity.LinkIsInvalidOrExpired);
            return;
        }       
    }
}
