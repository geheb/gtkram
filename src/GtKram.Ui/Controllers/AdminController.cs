using GtKram.Core.Email;
using GtKram.Core.Models.Bazaar;
using GtKram.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace GtKram.Ui.Controllers;

[ApiKey]
[ApiController]
[Route("/api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly SellerRegistrations _sellerRegistrations;
    private readonly BazaarEvents _bazaarEvents;
    private readonly EmailValidatorService _emailValidator;
    private readonly Users _users;

    public AdminController(
        SellerRegistrations sellerRegistrations, 
        BazaarEvents bazaarEvents,
        EmailValidatorService emailValidator,
        Users users)
    {
        _sellerRegistrations = sellerRegistrations;
        _bazaarEvents = bazaarEvents;
        _emailValidator = emailValidator;
        _users = users;
    }

    [HttpPost("registerseller")]
    public async Task<IActionResult> RegisterSeller([FromBody] RegisterSellerInput input, CancellationToken cancellationToken)
    {
        var @event = await _bazaarEvents.Find(input.EventId, cancellationToken);
        if (@event == null)
        {
            return StatusCode((int)HttpStatusCode.BadRequest, "event not found");
        }

        if (!await _emailValidator.Validate(input.Email!, cancellationToken))
        {
            return StatusCode((int) HttpStatusCode.BadRequest, "invalid email");
        }

        var dto = new BazaarSellerRegistrationDto()
        {
            Name = input.Name,
            Email = input.Email,
            Phone = input.Phone
        };

        var regId = await _sellerRegistrations.Register(@event.Id!.Value, dto, cancellationToken);

        if (!regId.HasValue)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, "register failed");
        }

        if (!await _sellerRegistrations.Confirm(@event.Id.Value, regId.Value, true, cancellationToken))
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, "confirm failed");
        }

        if (!await _sellerRegistrations.NotifyRegistration(regId.Value, cancellationToken))
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, "notify failed");
        }

        return StatusCode((int)HttpStatusCode.OK, true);
    }
}
