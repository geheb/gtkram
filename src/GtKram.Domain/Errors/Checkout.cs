using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class Checkout
{
    private const string _prefix = "checkout";

    public static Error NotFound { get; } =
        new($"{_prefix}.not.found", "Der Kassenvorgang wurde nicht gefunden.");

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Der Kassenvorgang konnte nicht gespeichert werden.");

    public static Error StatusCompleted { get; } =
        new($"{_prefix}.status.completed", "Der Kassenvorgang ist geschlossen.");

    public static Error AlreadyBooked { get; } =
        new($"{_prefix}.booked", "Der Artikel ist bereits gebucht.");

    public static Error Empty { get; } =
        new($"{_prefix}.empty", "Der Kassenvorgang ist leer.");
}
