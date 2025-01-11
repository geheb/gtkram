using GtKram.Core.Models.Bazaar;
using GtKram.Ui.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GtKram.Ui.Pages.Bazaars;

public class BazaarEventInput
{
    [Display(Name = "Name des Kinderbasars", Prompt = "z.b. Mein Kinderkram")]
    [RequiredField]
    [StringLength(128, MinimumLength = 4, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string Name { get; set; } = "Mein Kinderkram";

    [Display(Name = "Beschreibung")]
    [StringLength(1024, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? Description { get; set; }

    [Display(Name = "Startet am")]
    [RequiredField]
    public string StartDate { get; set; }

    [Display(Name = "Endet am")]
    [RequiredField]
    public string EndDate { get; set; }

    [Display(Name = "Adresse")]
    [StringLength(256, MinimumLength = 6, ErrorMessage = "Das Feld '{0}' muss mindestens {2} und höchstens {1} Zeichen enthalten.")]
    public string? Address { get; set; }

    [Display(Name = "Maximale Anzahl der Verkäufer")]
    [RequiredField]
    [Range(10, 200, ErrorMessage = "Das Feld '{0}' muss eine Zahl zwischen {1} und {2} sein.")]
    public int MaxSellers { get; set; } = 70;

    [Display(Name = "Registrierung startet am")]
    [RequiredField]
    public string RegisterStartDate { get; set; }

    [Display(Name = "Registrierung endet am")]
    [RequiredField]
    public string RegisterEndDate { get; set; }

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

    public BazaarEventInput()
    {
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

    public string? Validate()
    {
        var dc = new GermanDateTimeConverter();
        var startDate = dc.FromIsoDateTime(StartDate)!.Value;
        var endDate = dc.FromIsoDateTime(EndDate)!.Value;

        if (startDate >= endDate)
        {
            return "Das Datum für den Kinderbasar ist ungültig.";
        }

        var regStartDate = dc.FromIsoDateTime(RegisterStartDate)!.Value;
        var regEndDate = dc.FromIsoDateTime(RegisterEndDate)!.Value;

        if (regStartDate >= regEndDate || regStartDate >= startDate)
        {
            return "Die Registrierung der Verkäufer sollte vor dem Kinderbasars stattfinden.";
        }

        var articleEndDate = dc.FromIsoDateTime(EditArticleEndDate)!.Value;
        if (articleEndDate >= startDate)
        {
            return "Die Bearbeitung der Artikel sollte vor dem Kinderbasars stattfinden.";
        }
        else if (articleEndDate <= regEndDate)
        {
            return "Die Bearbeitung der Artikel sollte nach dem Datum für die Registrierung liegen.";
        }

        var pickUpStartDate = dc.FromIsoDateTime(PickUpLabelsStartDate)!.Value;
        var pickUpEndDate = dc.FromIsoDateTime(PickUpLabelsEndDate)!.Value;
        if (pickUpStartDate >= pickUpEndDate)
        {
            return "Das Datum für die Abholung der Etiketten ist ungültig.";
        }
        else if (pickUpStartDate >= startDate)
        {
            return "Die Abholung der Etiketten sollte vor dem Kinderbasar stattfinden.";
        }
        else if (pickUpStartDate <= articleEndDate)
        {
            return "Die Abholung der Etiketten sollte nach dem Datum für die Bearbeitung der Artikel liegen.";
        }

        return null;
    }

    public void From(BazaarEventDto dto)
    {
        var dc = new GermanDateTimeConverter();

        Name = dto.Name!;
        Description = dto.Description;
        StartDate = dc.ToIso(dto.StartDate);
        EndDate = dc.ToIso(dto.EndDate);
        Address = dto.Address!;
        MaxSellers = dto.MaxSellers;
        RegisterStartDate = dc.ToIso(dto.RegisterStartDate);
        RegisterEndDate = dc.ToIso(dto.RegisterEndDate);
        EditArticleEndDate = dto.EditArticleEndDate.HasValue ? dc.ToIso(dto.EditArticleEndDate.Value) : null;
        PickUpLabelsStartDate = dto.PickUpLabelsStartDate.HasValue ? dc.ToIso(dto.PickUpLabelsStartDate.Value) : null;
        PickUpLabelsEndDate = dto.PickUpLabelsEndDate.HasValue ? dc.ToIso(dto.PickUpLabelsEndDate.Value) : null;
        IsRegistrationsLocked = dto.IsRegistrationsLocked;
    }

    public void To(BazaarEventDto dto)
    {
        var dc = new GermanDateTimeConverter();

        dto.Name = Name;
        dto.Description = Description;
        dto.StartDate = dc.FromIsoDateTime(StartDate)!.Value;
        dto.EndDate = dc.FromIsoDateTime(EndDate)!.Value;
        dto.Address = Address;
        dto.MaxSellers = MaxSellers;
        dto.RegisterStartDate = dc.FromIsoDateTime(RegisterStartDate)!.Value;
        dto.RegisterEndDate = dc.FromIsoDateTime(RegisterEndDate)!.Value;
        dto.EditArticleEndDate = dc.FromIsoDateTime(EditArticleEndDate);
        dto.PickUpLabelsStartDate = dc.FromIsoDateTime(PickUpLabelsStartDate)!.Value;
        dto.PickUpLabelsEndDate = dc.FromIsoDateTime(PickUpLabelsEndDate)!.Value;
        dto.IsRegistrationsLocked = IsRegistrationsLocked;
    }
}
