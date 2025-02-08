namespace GtKram.Infrastructure.User;

using Microsoft.AspNetCore.Identity;

internal sealed class GermanyIdentityErrorDescriber : IdentityErrorDescriber
{
    private const string _prefix = "identity";

    public override IdentityError DefaultError() => new() { Code = $"{_prefix}.default.error", Description = $"Ein unbekannter Fehler ist aufgetreten." };
    public override IdentityError ConcurrencyFailure() => new() { Code = $"{_prefix}.concurrency.failure", Description = "Fehler bzgl. der optimistischen Nebenläufigkeit. Das Objekt wurde verändert." };
    public override IdentityError PasswordMismatch() => new() { Code = $"{_prefix}.password.mismatch", Description = "Ungültiges Passwort." };
    public override IdentityError InvalidToken() => new() { Code = $"{_prefix}.invalid.token", Description = "Ungültiges Token." };
    public override IdentityError RecoveryCodeRedemptionFailed() => new() { Code = $"{_prefix}.recovery.code.redemption.failed", Description = "Die Einlösung des Wiederherstellungscodes ist fehlgeschlagen." };
    public override IdentityError LoginAlreadyAssociated() => new() { Code = $"{_prefix}.login.already.associated", Description = "Es ist bereits ein Nutzer mit diesem Login vorhanden." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = $"{_prefix}.invalid.username", Description = $"Nutzername '{userName}' ist ungültig. Erlaubt sind nur Buchstaben und Zahlen." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = $"{_prefix}.invalid.email", Description = $"Die E-Mail-Adresse '{email}' ist ungültig." };
    public override IdentityError DuplicateUserName(string? userName) => new() { Code = $"{_prefix}.duplicate.username", Description = $"Nutzername '{userName}' ist bereits vergeben." };
    public override IdentityError DuplicateEmail(string? email) => new() { Code = $"{_prefix}.duplicate.email", Description = $"Die E-Mail-Adresse '{email}' ist bereits vergeben." };
    public override IdentityError InvalidRoleName(string? role) => new() { Code = $"{_prefix}.invalid.rolename", Description = $"Rollen-Name '{role}' ist ungültig." };
    public override IdentityError DuplicateRoleName(string? role) => new() { Code = $"{_prefix}.duplicate.rolename", Description = $"Rollen-Name '{role}' ist bereits vergeben." };
    public override IdentityError UserAlreadyHasPassword() => new() { Code = $"{_prefix}.user.already.has.password", Description = "Der Nutzer hat bereits ein Passwort gesetzt." };
    public override IdentityError UserLockoutNotEnabled() => new() { Code = $"{_prefix}.user.lockout.not.enabled", Description = "Aussperrung ist für diesen Nutzer nicht aktiviert." };
    public override IdentityError UserAlreadyInRole(string? role) => new() { Code = $"{_prefix}.user.already.in.role", Description = $"Nutzer hat bereits die Rolle '{role}'." };
    public override IdentityError UserNotInRole(string? role) => new() { Code = $"{_prefix}.user.not.in.role", Description = $"Der Nutzer ist nicht in der Rolle '{role}'." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = $"{_prefix}.password.too.short", Description = $"Passwörter müssen mindestens {length} Zeichen lang sein." };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = $"{_prefix}.password.requires.unique.chars", Description = $"Passwörter müssen mindestens {uniqueChars} verschiedene Zeichen enthalten." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = $"{_prefix}.password.requires.non.alphanumeric", Description = "Passwörter müssen mindestens ein Sonderzeichen enthalten." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = $"{_prefix}.password.requires.digit", Description = "Passwörter müssen mindestens eine Ziffer enthalten ('0'-'9')." };
    public override IdentityError PasswordRequiresLower() => new() { Code = $"{_prefix}.password.requires.lower", Description = "Passwörter müssen mindestens einen Kleinbuchstaben enthalten ('a'-'z')." };
    public override IdentityError PasswordRequiresUpper() => new() { Code = $"{_prefix}.password.requires.upper", Description = "Passwörter müssen mindestens einen Großbuchstaben enthalten ('A'-'Z')." };
}

