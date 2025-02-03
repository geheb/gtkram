using GtKram.Application.Converter;
using GtKram.Domain.Models;

namespace GtKram.Ui.Converter;

public sealed class EventConverter
{
    private readonly GermanDateTimeConverter _dateTimeConverter = new();

    public string Format(BazaarEvent model)
    {
        var nameAndDescription = model.Name + (string.IsNullOrEmpty(model.Description) ? string.Empty : (" - " + model.Description));
        return nameAndDescription + ", " + _dateTimeConverter.FormatShort(model.StartsOn, model.EndsOn);
    }

    public bool IsExpired(BazaarEvent model, TimeProvider timeProvider) =>
        _dateTimeConverter.ToLocal(timeProvider.GetUtcNow()) > model.EndsOn;

    public decimal CalcPayout(BazaarEvent model, decimal total) =>
        total - total * (model.Commission / 100.0M);
}
