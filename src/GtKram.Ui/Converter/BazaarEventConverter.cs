using GtKram.Application.Converter;
using GtKram.Domain.Models;

namespace GtKram.Ui.Converter;

public sealed class BazaarEventConverter
{
    private readonly GermanDateTimeConverter _dateTimeConverter = new();

    public string Format(BazaarEvent model)
    {
        var nameAndDescription = model.Name + (string.IsNullOrEmpty(model.Description) ? string.Empty : (" - " + model.Description));
        return nameAndDescription + ", " + _dateTimeConverter.FormatShort(model.StartsOn, model.EndsOn);
    }
}
