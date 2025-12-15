using GtKram.Application.UseCases.User.Commands;
using GtKram.Application.UseCases.User.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.WebApp.Pages.Login;

[Authorize]
public class ExitModel : PageModel
{
    private readonly IMediator _mediator;

    public ExitModel(IMediator mediator) => _mediator = mediator;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new SignOutCommand(User.GetId()), cancellationToken);
        return LocalRedirect("/");
    }
}
