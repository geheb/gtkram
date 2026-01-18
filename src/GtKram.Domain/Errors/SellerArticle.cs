using ErrorOr;

namespace GtKram.Domain.Errors;

public static class SellerArticle
{
    private const string _prefix = "seller.article";

    public static Error MultipleSaveFailed { get; } =
        Error.Failure($"{_prefix}.multiple.save.failed", "Die Artikel konnten nicht gespeichert werden.");

    public static Error NotFound { get; } =
        Error.NotFound($"{_prefix}.not.found", "Der Artikel wurde nicht gefunden.");

    public static Error DeleteFailed { get; } =
        Error.Failure($"{_prefix}.delete.failed", "Der Artikel konnte nicht gelöscht werden.");

    public static Error Empty { get; } =
        Error.Failure($"{_prefix}.empty", "Keine Artikel vorhanden.");

    public static Error MaxExceeded { get; } =
        Error.Failure($"{_prefix}.max.exceeded", "Die maximale Anzahl der Artikel wurde überschritten.");

    public static Error EditExpired { get; } =
        Error.Failure($"{_prefix}.edit.expired", "Die Bearbeitung der Artikel ist abgelaufen.");

    public static Error EditFailedDueToBooked { get; } =
        Error.Failure($"{_prefix}.edit.failed.due.to.booked", "Der Artikel ist bereits gebucht und kann nicht bearbeitet werden.");

    public static Error InvalidPriceRange { get; } =
        Error.Failure($"{_prefix}.invalid.price.range", "Der Preis sollte in 50-Cent-Schritten angegeben werden.");

    public static Error Timeout { get; } =
        Error.Failure($"{_prefix}.timeout", "Zeitüberschreitung beim Bearbeiten der Artikel. Bitte erneut versuchen.");
}
