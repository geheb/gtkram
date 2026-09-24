using ErrorOr;

namespace GtKram.Domain.Errors;

public static class Checkout
{
    private const string _prefix = "checkout";

    public static Error NotFound { get; } =
        Error.NotFound($"{_prefix}.not.found", "Der Kassenvorgang wurde nicht gefunden.");

    public static Error StatusCompleted { get; } =
        Error.Failure($"{_prefix}.status.completed", "Der Kassenvorgang ist geschlossen.");

    public static Error AlreadyBooked { get; } =
        Error.Failure($"{_prefix}.booked", "Der Artikel ist bereits gebucht.");

    public static Error Empty { get; } =
        Error.Failure($"{_prefix}.empty", "Der Kassenvorgang ist leer.");

    public static Error DeleteArticleFailed { get; } =
        Error.Failure($"{_prefix}.delete.article.failed", "Fehler beim Löschen des Artikels im Kassenvorgang.");

    public static Error AddArticleFailed { get; } =
        Error.Failure($"{_prefix}.add.article.failed", "Fehler beim Hinzufügen des Artikels im Kassenvorgang.");

    public static Error CompleteFailed { get; } =
        Error.Failure($"{_prefix}.complete.failed", "Fehler beim Abschließen des Kassenvorgangs.");

    public static Error WrongEvent { get; } =
        Error.Failure($"{_prefix}.wrong.event", "Der Artikel ist einem anderen Kinderbasar zugeordnet.");
}
