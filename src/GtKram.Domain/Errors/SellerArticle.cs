using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class SellerArticle
{
    private const string _prefix = "seller.article";

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Der Artikel konnte nicht gespeichert werden.");

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Der Artikel wurde nicht gefunden.");
}
