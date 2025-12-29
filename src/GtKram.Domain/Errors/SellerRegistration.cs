using ErrorOr;

namespace GtKram.Domain.Errors;

public static class SellerRegistration
{
    private const string _prefix = "seller.registration";

    public static Error NotFound { get; } =
        Error.NotFound($"{_prefix}.not.found", "Die Registrierung wurde nicht gefunden.");

    public static Error SaveFailed { get; } =
        Error.Failure($"{_prefix}.save.failed", "Die Registrierung konnte nicht gespeichert werden.");

    public static Error Timeout { get; } =
        Error.Failure($"{_prefix}.timeout", "Zeitüberschreitung beim Beareiten der Registrierung. Bitte erneut versuchen.");

    public static Error IsLocked { get; } =
        Error.Failure($"{_prefix}.is.locked", "Die Registrierung ist aktuell gesperrt.");

    public static Error IsExpired { get; } =
        Error.Failure($"{_prefix}.is.expired", "Die Registrierung ist bereits abgelaufen.");

    public static Error LimitExceeded { get; } =
        Error.Failure($"{_prefix}.limit.exceeded", "Die maximale Anzahl von Registrierungen wurde erreicht.");
}
