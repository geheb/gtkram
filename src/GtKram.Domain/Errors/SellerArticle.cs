using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class SellerArticle
{
    private const string _prefix = "seller.article";

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Der Artikel konnte nicht gespeichert werden.");

    public static Error MultipleSaveFailed { get; } =
        new($"{_prefix}.multiple.save.failed", "Die Artikel konnten nicht gespeichert werden.");

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Der Artikel wurde nicht gefunden.");

    public static Error IsEmpty { get; } =
        new($"{_prefix}.is.empty", "Keine Artikel vorhanden.");

    public static Error MaxExceeded { get; } =
        new($"{_prefix}.max.exceeded", "Die maximale Anzahl der Artikel wurde überschritten.");

    public static Error EditExpired { get; } =
        new($"{_prefix}.edit.expired", "Die Bearbeitung des Artikels ist abgelaufen.");

    public static Error EditFailedDueToBooked { get; } =
        new($"{_prefix}.edit.failed.due.to.booked", "Der Artikel ist bereits gebucht und kann nicht bearbeitet werden.");

    public static Error InvalidPriceRange { get; } =
        new($"{_prefix}.invalid.price.range", "Der Preis sollte in 50-Cent-Schritten angegeben werden.");

    public static Error Timeout { get; } =
        new($"{_prefix}.timeout", "Zeitüberschreitung beim Bearbeiten der Artikel. Bitte erneut versuchen.");
}
