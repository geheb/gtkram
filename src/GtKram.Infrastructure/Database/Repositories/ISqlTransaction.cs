namespace GtKram.Infrastructure.Database.Repositories;

internal interface ISqlTransaction : IAsyncDisposable
{
    Task Commit(CancellationToken cancellationToken);
}
