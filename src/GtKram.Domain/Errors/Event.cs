using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class Event
{
    private const string _prefix = "event";

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Der Kinderbasar wurde nicht gefunden.");

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Der Kinderbasar konnte nicht gespeichert werden.");

    public static Error Expired { get; } =
        new($"{_prefix}.expired", "Der Kinderbasar ist bereits abgelaufen.");

    public static Error ValidationDeleteNotPossibleDueToRegistrations { get; } =
        new($"{_prefix}.validation.delete.failed", "Der Kinderbasar kann nicht gelöscht werden, da Registrierungen vorliegen.");

    public static Error ValidationDateFailed { get; } =
        new($"{_prefix}.validation.date.failed", "Das Datum für den Kinderbasar ist ungültig.");

    public static Error ValidationRegisterDateFailed { get; } =
        new($"{_prefix}.validation.register.date.failed", "Die Registrierung der Verkäufer sollte vor dem Datum des Kinderbasars stattfinden.");

    public static Error ValidationEditArticleDateBeforeFailed { get; } =
        new($"{_prefix}.validation.edit.articles.date.before.failed", "Die Bearbeitung der Artikel sollte vor dem Datum des Kinderbasars stattfinden.");

    public static Error ValidationEditArticleDateAfterFailed { get; } =
        new($"{_prefix}.validation.edit.articles.date.after.failed", "Die Bearbeitung der Artikel sollte nach dem Datum für die Registrierung stattfinden.");

    public static Error ValidationPickUpLabelDateFailed { get; } =
        new($"{_prefix}.validation.pickup.label.date.failed", "Das Datum für die Abholung der Etiketten ist ungültig.");

    public static Error ValidationPickupLabelDateBeforeFailed { get; } =
        new($"{_prefix}.validation.pickup.label.date.before.failed", "Die Abholung der Etiketten sollte vor dem Datum des Kinderbasars stattfinden.");

    public static Error ValidationPickupLabelDateAfterFailed { get; } =
        new($"{_prefix}.validation.pickup.label.date.after.failed", "Die Abholung der Etiketten sollte nach dem Datum für die Bearbeitung der Artikel stattfinden.");
}
