using GtKram.Domain.Models;

namespace GtKram.Application.Converter;

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

    public bool IsEditArticlesExpired(BazaarEvent model, TimeProvider timeProvider) =>
        model.EditArticleEndsOn is not null &&
        _dateTimeConverter.ToLocal(timeProvider.GetUtcNow()) > model.EditArticleEndsOn;

    public decimal CalcPayout(BazaarEvent model, decimal total) =>
        total - total * (model.Commission / 100.0M);

    public bool IsRegisterExpired(BazaarEvent model, TimeProvider timeProvider)
    {
        var now = _dateTimeConverter.ToLocal(timeProvider.GetUtcNow());
        return !model.IsRegistrationsLocked 
            && now >= model.RegisterStartsOn 
            && now <= model.RegisterEndsOn;
    }
}
