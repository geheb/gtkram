using GtKram.Domain.Models;

namespace GtKram.Application.Converter;

public sealed class EventConverter
{
    private readonly GermanDateTimeConverter _dateTimeConverter = new();

    public string Format(Event model)
    {
        var nameAndDescription = model.Name + (string.IsNullOrEmpty(model.Description) ? string.Empty : (" - " + model.Description));
        return nameAndDescription + ", " + _dateTimeConverter.FormatShort(model.Start, model.End);
    }

    public bool IsExpired(Event model, TimeProvider timeProvider) =>
        _dateTimeConverter.ToLocal(timeProvider.GetUtcNow()) > model.End;

    public bool IsEditArticlesExpired(Event model, TimeProvider timeProvider) =>
        model.EditArticleEnd is not null &&
        _dateTimeConverter.ToLocal(timeProvider.GetUtcNow()) > model.EditArticleEnd;

    public decimal CalcPayout(Event model, decimal total) =>
        total - total * (model.Commission / 100.0M);

    public bool IsRegisterExpired(Event model, TimeProvider timeProvider)
    {
        var now = _dateTimeConverter.ToLocal(timeProvider.GetUtcNow());
        return
            now >= model.RegisterStart 
            && now <= model.RegisterEnd;
    }
}
