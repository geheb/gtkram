using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class Internal
{
    private const string _prefix = "internal";

    public static Error EmailNotFound { get; } =
        new($"{_prefix}.not.found", "Die Email wurde nicht gefunden.");

    public static Error EmailSaveFailed { get; } =
        new($"{_prefix}.save.failed", "Die Email konnte nicht gespeichert werden.");

    public static Error CreateKeyFailed { get; } =
        new($"{_prefix}.create.key.failed", "Fehler beim Erstellen des geheimen Schlüssels.");

    public static Error InvalidCode { get; } =
        new($"{_prefix}.invalid.code", "Der Code ist ungültig.");
}
