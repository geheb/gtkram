using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Application.UseCases.Bazaar.Queries;
using GtKram.Application.UseCases.User.Extensions;
using GtKram.Ui.Extensions;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GtKram.Ui.Pages.MyCheckouts;

[Node("Artikel anlegen", FromPage = typeof(ArticlesModel))]
[Authorize(Roles = "checkout")]
public class ArticleAddModel : PageModel
{
    private readonly TimeProvider _timeProvider;
    private readonly IMediator _mediator;

    [BindProperty]
    public ArticleInput Input { get; set; } = new();

    public bool IsDisabled { get; set; }

    public ArticleAddModel(
        TimeProvider timeProvider,
        IMediator mediator)
    {
        _timeProvider = timeProvider;
        _mediator = mediator;
    }

    public async Task OnGetAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindEventByCheckoutQuery(id), cancellationToken);
        if (result.IsFailed)
        {
            IsDisabled = true;
            ModelState.AddError(result.Errors);
            return;
        }

        var eventConverter = new EventConverter();
        Input.State_Event = eventConverter.Format(result.Value);

        if (eventConverter.IsExpired(result.Value, _timeProvider))
        {
            IsDisabled = true;
            ModelState.AddError(Domain.Errors.Event.Expired);
            return;
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid eventId, Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _mediator.Send(
            new CreateCheckoutArticleManuallyByUserCommand(User.GetId(), id, Input.SellerNumber!.Value, Input.LabelNumber!.Value),
            cancellationToken);

        if (result.IsFailed)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        return RedirectToPage("Articles", new { eventId, id });
    }
}
