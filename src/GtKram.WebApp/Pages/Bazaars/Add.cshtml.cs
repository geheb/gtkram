using GtKram.Application.Options;
using GtKram.Infrastructure.AspNetCore.Extensions;
using GtKram.Infrastructure.AspNetCore.Routing;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace GtKram.WebApp.Pages.Bazaars;

[Node("Anlegen", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin")]
public sealed class AddModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty]
    public BazaarEventInput Input { get; set; } = new();

    public AddModel(
        IMediator mediator, 
        IOptions<AppSettings> appSettings)
    {
        _mediator = mediator;
        Input.InitDefault(appSettings.Value.DefaultEventLocation);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();

        var result = await _mediator.Send(Input.ToCreateCommand(), cancellationToken);
        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Index");
    }
}
