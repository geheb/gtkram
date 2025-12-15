namespace GtKram.Infrastructure.Database;

internal sealed class PkGenerator
{
    public Guid Generate() => Guid.CreateVersion7(DateTimeOffset.UtcNow);
}
