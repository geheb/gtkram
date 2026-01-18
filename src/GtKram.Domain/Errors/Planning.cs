using ErrorOr;

namespace GtKram.Domain.Errors;

public static class Planning
{
    private const string _prefix = "planning";

    public static Error NotFound { get; } =
        Error.NotFound($"{_prefix}.not.found", "Die Planung wurde nicht gefunden.");

    public static Error ValidationDateFailed { get; } =
        Error.Validation($"{_prefix}.validation.date.failed", "Das Datum für die Planung ist ungültig.");

    public static Error ValidationFromBeforeToFailed { get; } =
        Error.Validation($"{_prefix}.validation.from.before.to.failed", "Der Von-Zeitpunkt sollte vor dem Bis-Zeitpunkt liegen.");

}
