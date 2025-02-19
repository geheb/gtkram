using GtKram.Application.Options;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace GtKram.Ui.Pages.Bazaars;

[Node("Anlegen", FromPage = typeof(IndexModel))]
[Authorize(Roles = "manager,admin")]
public class AddModel : PageModel
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

        var result = await _mediator.Send(Input.ToCommand(), cancellationToken);
        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Index");
    }
}
