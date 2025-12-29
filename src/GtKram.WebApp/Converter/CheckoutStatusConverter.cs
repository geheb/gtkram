using GtKram.Domain.Models;

namespace GtKram.WebApp.Converter;

public sealed class CheckoutStatusConverter
{
    public string StatusToString(CheckoutStatus status)
    {
        return status switch
        {
            CheckoutStatus.InProgress => "In Bearbeitung",
            CheckoutStatus.Completed => "Abgeschlossen",
            _ => $"Unbekannt: {status}"
        };
    }
}
