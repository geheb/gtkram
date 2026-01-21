using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Domain.Models;
using GtKram.Infrastructure.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.WebApp.Pages.Plannings;

public sealed class ScheduleInput
{
    public Guid State_EventId { get; set; }
    public string? State_Event { get; set; }
    public ICollection<Guid> UserIds { get; set; } = [];
    public ICollection<Guid> CheckedUserIds { get; set; } = [];
    public ICollection<string> Persons { get; set; } = [];
    public ICollection<string> CheckedPersons { get; set; } = [];

    [Display(Name = "Name")]
    [RequiredField, TextLengthField]
    public string? Name { get; set; }

    [Display(Name = "Datum")]
    [RequiredField]
    public string? Date { get; set; }

    [Display(Name = "Helfer maximal")]
    [RangeField(1, 100)]
    public int? MaxHelper { get; set; }

    [Display(Name = "Von")]
    [RequiredField]
    public string? From { get; set; }

    [Display(Name = "Bis")]
    [RequiredField]
    public string? To { get; set; }

    internal void Init(Planning model)
    {
        var dc = new GermanDateTimeConverter();
        Name = model.Name;
        Date = dc.ToIso(DateOnly.FromDateTime(model.Date.DateTime));
        From = dc.ToIso(model.From);
        To = dc.ToIso(model.To);
        MaxHelper = model.MaxHelper;
        UserIds = model.IdentityIds;
        CheckedUserIds = model.CheckedIdentityIds;
        Persons = model.Persons;
        CheckedPersons = model.CheckedPersons;
    }

    internal CreatePlanningCommand ToCreateCommand(Guid eventId) 
    {
        var dc = new GermanDateTimeConverter();
        var date = dc.FromIsoDate(Date);
        return new(new()
        {
            EventId = eventId,
            Name = Name!,
            Date = date is not null ? dc.ToUtc(date.Value) : DateTimeOffset.MinValue,
            From = dc.FromIsoTime(From) ?? TimeOnly.MinValue,
            To = dc.FromIsoTime(To) ?? TimeOnly.MinValue,
            MaxHelper = MaxHelper,
            IdentityIds = new HashSet<Guid>(UserIds),
            Persons = new HashSet<string>(Persons, StringComparer.OrdinalIgnoreCase)
        });
    }

    internal UpdatePlanningCommand ToUpdateCommand(Guid id, Guid eventId)
    {
        var dc = new GermanDateTimeConverter();
        var date = dc.FromIsoDate(Date);
        return new(new()
        {
            Id = id,
            EventId = eventId,
            Name = Name!,
            Date = date is not null ? dc.ToUtc(date.Value) : DateTimeOffset.MinValue,
            From = dc.FromIsoTime(From) ?? TimeOnly.MinValue,
            To = dc.FromIsoTime(To) ?? TimeOnly.MinValue,
            MaxHelper = MaxHelper,
            IdentityIds = new HashSet<Guid>(UserIds),
            CheckedIdentityIds = new HashSet<Guid>(CheckedUserIds),
            Persons = new HashSet<string>(Persons, StringComparer.OrdinalIgnoreCase),
            CheckedPersons = new HashSet<string>(CheckedPersons, StringComparer.OrdinalIgnoreCase),
        });
    }
}
