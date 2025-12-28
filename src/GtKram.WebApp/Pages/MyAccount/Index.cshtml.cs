using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Application.UseCases.User.Queries;
using GtKram.Infrastructure.AspNetCore.Annotations;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Pages.MyAccount;

[Node("Mein Konto", FromPage = typeof(Pages.IndexModel))]
[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IMediator _mediator;

    [Display(Name = "E-Mail-Adresse")]
    public string? Email { get; set; }

    [BindProperty, Display(Name = "Name")]
    [RequiredField, TextLengthField]
    public string? Name { get; set; }

    public bool IsDisabled { get; set; }

    public string? Info { get; set; }

    public IndexModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task OnGetAsync(int? message, CancellationToken cancellationToken)
    {
        Info = message switch
        {
            1 => "Das Passwort wurde geändert.",
            2 => "Wir haben eine E-Mail an deine neue E-Mail-Adresse gesendet.",
            3 => "2FA wurde aktiviert. Bitte erneut anmelden!",
            4 => "2FA wurde deaktiviert.",
            _ => default
        };

        return Update(cancellationToken);
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        if (!await Update(cancellationToken)) return;

        var result = await _mediator.Send(new UpdateUserCommand(User.GetId(), Name!, null), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return;
        }

        Info = "Änderungen wurden gespeichert.";
    }

    private async Task<bool> Update(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindUserByIdQuery(User.GetId()), cancellationToken);
        if (result.IsError)
        {
            IsDisabled = true;
            return false;
        }

        Name = result.Value.Name;
        Email = result.Value.Email;

        return ModelState.IsValid;
    }
}
