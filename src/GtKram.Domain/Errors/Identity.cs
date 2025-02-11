using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class Identity
{
    private const string _prefix = "identity";

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Der Nutzer wurde nicht gefunden.");

    public static Error TwoFactorNotEnabled { get; } =
        new($"{_prefix}.two.factor.auth.not.enabled", "Die Zwei-Faktor-Authentifizierung (2FA) ist nicht eingerichtet.");

    public static Error AlreadyActivated { get; } =
        new($"{_prefix}.already.activated", "Der Nutzer wurde bereits bestätigt.");

    public static Error Locked { get; } =
        new($"{_prefix}.locked", "Der Nutzer ist gesperrt.");

    public static Error LoginNotAllowed { get; } =
        new($"{_prefix}.login.not.allowed", "Der Nutzer darf sich nicht anmelden.");

    public static Error LoginFailed { get; } =
        new($"{_prefix}.login.failed", "Die Anmeldung ist fehlgeschlagen.");
}
