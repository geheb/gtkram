using ErrorOr;

namespace GtKram.Domain.Errors;

public static class Identity
{
    private const string _prefix = "identity";

    public static Error NotFound { get; } =
        Error.NotFound($"{_prefix}.not.found", "Der Nutzer wurde nicht gefunden.");

    public static Error DeleteFailed { get; } =
        Error.Failure($"{_prefix}.delete.failed", "Der Nutzer konnte nicht gelöscht werden.");

    public static Error TwoFactorNotEnabled { get; } =
        Error.Failure($"{_prefix}.two.factor.auth.not.enabled", "Die Zwei-Faktor-Authentifizierung (2FA) ist nicht eingerichtet.");

    public static Error AlreadyActivated { get; } =
        Error.Failure($"{_prefix}.already.activated", "Der Nutzer wurde bereits bestätigt.");

    public static Error Locked { get; } =
        Error.Failure($"{_prefix}.locked", "Der Nutzer ist gesperrt.");

    public static Error LoginNotAllowed { get; } =
        Error.Unauthorized($"{_prefix}.login.not.allowed", "Der Nutzer darf sich nicht anmelden.");

    public static Error LoginFailed { get; } =
        Error.Unauthorized($"{_prefix}.login.failed", "Die Anmeldung ist fehlgeschlagen.");

    public static Error LinkIsExpired { get; } =
       Error.Failure($"{_prefix}.link.is.expired", "Der Link ist ungültig oder abgelaufen.");
}
