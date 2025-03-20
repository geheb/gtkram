using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class EventRegistration
{
    private const string _prefix = "event.registration";

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Die Registrierung wurde nicht gefunden.");

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Die Registrierung konnte nicht gespeichert werden.");

    public static Error Timeout { get; } =
        new($"{_prefix}.timeout", "Zeitüberschreitung beim Beareiten der Registrierung. Bitte erneut versuchen.");

    public static Error Expired { get; } =
        new($"{_prefix}.expired", "Die Registrierung ist bereits abgelaufen.");

    public static Error LimitExceeded { get; } =
        new($"{_prefix}.limit.exceeded", "Die maximale Anzahl von Registrierungen wurde erreicht.");
}
