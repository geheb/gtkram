using GtKram.Domain.Models;

namespace GtKram.Ui.Converter;

public sealed class BillingStatusConverter
{
    public string StatusToString(BillingStatus status)
    {
        return status switch
        {
            BillingStatus.InProgress => "In Bearbeitung",
            BillingStatus.Completed => "Abgeschlossen",
            _ => $"Unbekannt: {status}"
        };
    }
}
