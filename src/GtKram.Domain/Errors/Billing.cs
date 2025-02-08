using GtKram.Domain.Base;

namespace GtKram.Domain.Errors;

public static class Billing
{
    private const string _prefix = "billing";

    public static Error SaveFailed { get; } =
        new($"{_prefix}.save.failed", "Der Kassenvorgang konnte nicht gespeichert werden.");
}
