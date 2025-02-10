using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class SellerArticle
{
    private const string _prefix = "seller.article";

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Der Artikel konnte nicht gespeichert werden.");

    public static Error MulitpleSaveFailed { get; } =
        new($"{_prefix}.multiple.save.failed", "Die Artikel konnten nicht gespeichert werden.");

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Der Artikel wurde nicht gefunden.");

    public static Error NotAvailable { get; } =
        new($"{_prefix}.not.available", "Keine Artikel vorhanden.");

    public static Error MaxExceeded { get; } =
        new($"{_prefix}.max.exceeded", "Die maximale Anzahl der Artikel wurde überschritten.");
}
