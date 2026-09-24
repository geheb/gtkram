namespace GtKram.Infrastructure.Database.Repositories;

public interface IRepoTransaction : IAsyncDisposable
{
    Task Commit(CancellationToken cancellationToken);
}
