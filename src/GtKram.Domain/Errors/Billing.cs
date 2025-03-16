using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class Billing
{
    private const string _prefix = "billing";

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Der Kassenvorgang wurde nicht gefunden.");

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Der Kassenvorgang konnte nicht gespeichert werden.");

    public static Error StatusCompleted { get; } =
        new($"{_prefix}.status.completed", "Der Kassenvorgang ist geschlossen.");

    public static Error IsEmpty { get; } =
        new($"{_prefix}.is.empty", "Der Kassenvorgang ist leer.");
}
