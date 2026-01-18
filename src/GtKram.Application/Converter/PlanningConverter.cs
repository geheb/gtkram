using GtKram.Domain.Models;

namespace GtKram.Application.Converter;

public sealed class PlanningConverter
{
    private readonly GermanDateTimeConverter _dateTimeConverter = new();

    public string Format(Planning model)
    {
        return
            model.Name + ", " +
            _dateTimeConverter.ToDate(model.Date) + " " +
            _dateTimeConverter.ToIso(model.From) + "-" + _dateTimeConverter.ToIso(model.To);
    }
}
