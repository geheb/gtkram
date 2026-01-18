using ErrorOr;

namespace GtKram.Domain.Errors;

public static class Internal
{
    private const string _prefix = "internal";

    public static Error InvalidRequest { get; } =
        Error.Validation($"{_prefix}.invalid.request", "Ungültige Anfrage.");

    public static Error Timeout { get; } =
        Error.Conflict($"{_prefix}.timeout", "Die Bearbeitung dieser Anfrage dauert zu lange, sodass sie vom Server abgebrochen wurde.");

    public static Error ConflictData { get; } =
        Error.Conflict($"{_prefix}.conflict.data", "Es gibt einen Datenkonflikt. Die Daten haben sich zwischenzeitlich geändert.");

    public static Error InvalidData { get; } =
        Error.Failure($"{_prefix}.invalid.data", "Ungültige Datenkonsistenz festgestellt.");

    public static Error EmailNotFound { get; } =
        Error.NotFound($"{_prefix}.not.found", "Die Email wurde nicht gefunden.");

    public static Error EmailSaveFailed { get; } =
        Error.Failure($"{_prefix}.save.failed", "Die Email konnte nicht gespeichert werden.");

    public static Error CreateKeyFailed { get; } =
        Error.Failure($"{_prefix}.create.key.failed", "Fehler beim Erstellen des geheimen Schlüssels.");

    public static Error InvalidCode { get; } =
        Error.Validation($"{_prefix}.invalid.code", "Der Code ist ungültig.");
}
