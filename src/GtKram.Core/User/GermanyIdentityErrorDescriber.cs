namespace GtKram.Core.User;

using Microsoft.AspNetCore.Identity;

internal sealed class GermanyIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = $"Ein unbekannter Fehler ist aufgetreten." };
    public override IdentityError ConcurrencyFailure() => new() { Code = nameof(ConcurrencyFailure), Description = "Fehler bzgl. der optimistischen Nebenläufigkeit. Das Objekt wurde verändert." };
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "Ungültiges Passwort." };
    public override IdentityError InvalidToken() => new() { Code = nameof(InvalidToken), Description = "Ungültiges Token." };
    public override IdentityError RecoveryCodeRedemptionFailed() => new() { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Die Einlösung des Wiederherstellungscodes ist fehlgeschlagen." };
    public override IdentityError LoginAlreadyAssociated() => new() { Code = nameof(LoginAlreadyAssociated), Description = "Es ist bereits ein Nutzer mit diesem Login vorhanden." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"Nutzername '{userName}' ist ungültig. Erlaubt sind nur Buchstaben und Zahlen." };
    public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"E-Mail '{email}' ist ungültig." };
    public override IdentityError DuplicateUserName(string? userName) => new() { Code = nameof(DuplicateUserName), Description = $"Nutzername '{userName}' ist bereits vergeben." };
    public override IdentityError DuplicateEmail(string? email) => new() { Code = nameof(DuplicateEmail), Description = $"E-Mail '{email}' ist bereits vergeben." };
    public override IdentityError InvalidRoleName(string? role) => new() { Code = nameof(InvalidRoleName), Description = $"Rollen-Name '{role}' ist ungültig." };
    public override IdentityError DuplicateRoleName(string? role) => new() { Code = nameof(DuplicateRoleName), Description = $"Rollen-Name '{role}' ist bereits vergeben." };
    public override IdentityError UserAlreadyHasPassword() => new() { Code = nameof(UserAlreadyHasPassword), Description = "Nutzer hat bereits ein Passwort gesetzt." };
    public override IdentityError UserLockoutNotEnabled() => new() { Code = nameof(UserLockoutNotEnabled), Description = "Aussperrung ist für diesen Nutzer nicht aktiviert." };
    public override IdentityError UserAlreadyInRole(string? role) => new() { Code = nameof(UserAlreadyInRole), Description = $"Nutzer hat bereits die Rolle '{role}'." };
    public override IdentityError UserNotInRole(string? role) => new() { Code = nameof(UserNotInRole), Description = $"Nutzer ist nicht in der Rolle '{role}'." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Passwörter müssen mindestens {length} Zeichen lang sein." };
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Passwörter müssen mindestens {uniqueChars} verschiedene Zeichen enthalten." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Passwörter müssen mindestens ein Sonderzeichen enthalten." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Passwörter müssen mindestens eine Ziffer enthalten ('0'-'9')." };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Passwörter müssen mindestens einen Kleinbuchstaben enthalten ('a'-'z')." };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "Passwörter müssen mindestens einen Großbuchstaben enthalten ('A'-'Z')." };
}

