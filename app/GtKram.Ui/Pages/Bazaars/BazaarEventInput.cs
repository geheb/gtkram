using GtKram.Application.Converter;
using GtKram.Application.UseCases.Bazaar.Commands;
using GtKram.Domain.Models;
using GtKram.Ui.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Bazaars;

public class BazaarEventInput
{
    [Display(Name = "Name des Kinderbasars", Prompt = "z.b. Mein Kinderkram")]
    [RequiredField]
    [StringLength(128, MinimumLength = 4, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? Name { get; set; }

    [Display(Name = "Beschreibung")]
    [StringLength(1024, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? Description { get; set; }

    [Display(Name = "Startet am")]
    [RequiredField]
    public string? StartDate { get; set; }

    [Display(Name = "Endet am")]
    [RequiredField]
    public string? EndDate { get; set; }

    [Display(Name = "Adresse")]
    [StringLength(256, MinimumLength = 6, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? Address { get; set; }

    [Display(Name = "Maximale Anzahl der Verkäufer")]
    [RequiredField]
    [Range(10, 200, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int MaxSellers { get; set; }

    [Display(Name = "Registrierung startet am")]
    [RequiredField]
    public string? RegisterStartDate { get; set; }

    [Display(Name = "Registrierung endet am")]
    [RequiredField]
    public string? RegisterEndDate { get; set; }

    [Display(Name = "Bearbeitung der Artikel endet am")]
    [RequiredField]
    public string? EditArticleEndDate { get; set; }

    [Display(Name = "Abholung der Etiketten startet am")]
    [RequiredField]
    public string? PickUpLabelsStartDate { get; set; }

    [Display(Name = "Abholung der Etiketten endet am")]
    [RequiredField]
    public string? PickUpLabelsEndDate { get; set; }

    [Display(Name = "Registrierungen sind gesperrt")]
    public bool IsRegistrationsLocked { get; set; }

    public void InitDefault(string? address)
    {
        Name = "Mein Kinderkram";
        Address = address;
        MaxSellers = 70;

        var dc = new GermanDateTimeConverter();
        var now = dc.ToLocal(DateTimeOffset.UtcNow);

        var baseStartDate = now.AddDays(7);
        StartDate = dc.ToIso(new DateTime(baseStartDate.Year, baseStartDate.Month, baseStartDate.Day, 13, 0, 0));
        EndDate = dc.ToIso(new DateTime(baseStartDate.Year, baseStartDate.Month, baseStartDate.Day, 17, 0, 0));

        RegisterStartDate = dc.ToIso(now);
        RegisterEndDate = dc.ToIso(now.AddDays(3));

        EditArticleEndDate = dc.ToIso(baseStartDate.AddDays(-3));

        PickUpLabelsStartDate = dc.ToIso(baseStartDate.AddDays(-1));
        PickUpLabelsEndDate = dc.ToIso(baseStartDate.AddDays(-1).AddHours(1));
    }

    public void Init(BazaarEvent model)
    {
        var dc = new GermanDateTimeConverter();
        Name = model.Name;
        Description = model.Description;
        StartDate = dc.ToIso(model.StartsOn);
        EndDate = dc.ToIso(model.EndsOn);
        Address = model.Address;
        MaxSellers = model.MaxSellers;
        RegisterStartDate = dc.ToIso(model.RegisterStartsOn);
        RegisterEndDate = dc.ToIso(model.RegisterEndsOn);
        EditArticleEndDate = model.EditArticleEndsOn is not null ? dc.ToIso(model.EditArticleEndsOn.Value) : null;
        PickUpLabelsStartDate = model.PickUpLabelsStartsOn is not null ? dc.ToIso(model.PickUpLabelsStartsOn.Value) : null;
        PickUpLabelsEndDate = model.PickUpLabelsEndsOn is not null ? dc.ToIso(model.PickUpLabelsEndsOn.Value) : null;
        IsRegistrationsLocked = model.IsRegistrationsLocked;
    }

    public CreateEventCommand ToCommand()
    {
        var dc = new GermanDateTimeConverter();
        return new(new()
        {
            Name = Name!,
            Description = Description,
            StartsOn = dc.FromIsoDateTime(StartDate)!.Value,
            EndsOn = dc.FromIsoDateTime(EndDate)!.Value,
            Address = Address,
            MaxSellers = MaxSellers,
            RegisterStartsOn = dc.FromIsoDateTime(RegisterStartDate)!.Value,
            RegisterEndsOn = dc.FromIsoDateTime(RegisterEndDate)!.Value,
            EditArticleEndsOn = dc.FromIsoDateTime(EditArticleEndDate)!.Value,
            PickUpLabelsStartsOn = dc.FromIsoDateTime(PickUpLabelsStartDate)!.Value,
            PickUpLabelsEndsOn = dc.FromIsoDateTime(PickUpLabelsEndDate)!.Value,
            IsRegistrationsLocked = IsRegistrationsLocked
        });
    }
}
