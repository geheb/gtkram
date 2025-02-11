using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class Seller
{
    private const string _prefix = "seller";

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Der Verkäufer konnte nicht gespeichert werden.");

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Der Verkäufer wurde nicht gefunden.");

    public static Error Locked { get; } =
        new($"{_prefix}.locked", "Der Verkäufer ist gesperrt.");
}
