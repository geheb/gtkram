using ErrorOr;

namespace GtKram.Domain.Errors;

public static class Seller
{
    private const string _prefix = "seller";

    public static Error SaveFailed { get; } =
        Error.Failure($"{_prefix}.save.failed", "Der Verkäufer konnte nicht gespeichert werden.");

    public static Error NotFound { get; } =
        Error.NotFound($"{_prefix}.not.found", "Der Verkäufer wurde nicht gefunden.");

    public static Error Locked { get; } =
        Error.Failure($"{_prefix}.locked", "Der Verkäufer ist gesperrt.");

    public static Error CheckoutNotAllowed { get; } =
        Error.Failure($"{_prefix}.checkout.not.allowed", "Das Anlegen der Kassenvorgänge ist nicht erlaubt.");
}
