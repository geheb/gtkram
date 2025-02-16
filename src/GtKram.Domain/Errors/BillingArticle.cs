using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class BillingArticle
{
    private const string _prefix = "billing.article";

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Der Kassenartikel konnte nicht gespeichert werden.");

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Der Kassenartikel wurde nicht gefunden.");

    public static Error DeleteFailed { get; } =
        new($"{_prefix}.save.failed", "Der Kassenartikel konnte nicht gelöscht werden.");
}
