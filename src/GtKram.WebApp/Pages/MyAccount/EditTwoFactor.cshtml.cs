using ErrorOr;
using GtKram.Application.Options;
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
using QRCoder;
using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Pages.MyAccount;

[Node("2FA bearbeiten", FromPage = typeof(IndexModel))]
[Authorize]
public sealed class EditTwoFactorModel : PageModel
{
    private readonly IMediator _mediator;

    [BindProperty, Display(Name = "6-stelliger Code aus der Authenticator-App")]
    [RequiredField, TextLengthField(6, MinimumLength = 6)]
    public string? Code { get; set; }

    [Display(Name = "Geheimer Schlüssel")]
    public string? SecretKey { get; set; }

    public string? AuthUri { get; set; }
    public bool IsTwoFactorEnabled { get; set; }

    public string? AuthQrCodeEncoded { get; set; }

    public EditTwoFactorModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var result2fa = await _mediator.Send(new GetOtpQuery(User.GetId()), cancellationToken);
        if (result2fa.IsError)
        {
            result2fa = await _mediator.Send(new CreateOtpCommand(User.GetId()), cancellationToken);
        }

        if (result2fa.IsError)
        {
            ModelState.AddError(result2fa.Errors);
        }
        else
        {
            IsTwoFactorEnabled = result2fa.Value.IsEnabled;
            SecretKey = result2fa.Value.SecretKey;
            AuthUri = result2fa.Value.AuthUri;
            AuthQrCodeEncoded = GenerateQrCodeEncoded(result2fa.Value.AuthUri);
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result2fa = await _mediator.Send(new GetOtpQuery(User.GetId()), cancellationToken);
        if (result2fa.IsError)
        {
            ModelState.AddError(result2fa.Errors);
            return Page();
        }

        IsTwoFactorEnabled = result2fa.Value.IsEnabled;
        SecretKey = result2fa.Value.SecretKey;
        AuthUri = result2fa.Value.AuthUri;
        AuthQrCodeEncoded = GenerateQrCodeEncoded(result2fa.Value.AuthUri);

        ErrorOr<Success> result;
        if (IsTwoFactorEnabled)
        {
            result = await _mediator.Send(new DisableOtpCommand(User.GetId(), Code!), cancellationToken);
        }
        else
        {
            result = await _mediator.Send(new EnableOtpCommand(User.GetId(), Code!), cancellationToken);
        }

        if (result.IsError)
        {
            ModelState.AddError(result.Errors);
            return Page();
        }

        if (!IsTwoFactorEnabled)
        {
            Response.Cookies.Delete(CookieNames.TwoFactorTrustToken);
        }

        return RedirectToPage("Index", new { message = IsTwoFactorEnabled ? 4 : 3 });
    }

    private static string GenerateQrCodeEncoded(string data)
    {
        using var generator = new QRCodeGenerator();
        using var code = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.H);
        using var image = new PngByteQRCode(code);
        var content = image.GetGraphic(5);
        return "data:image/png;base64," + Convert.ToBase64String(content);
    }
}
