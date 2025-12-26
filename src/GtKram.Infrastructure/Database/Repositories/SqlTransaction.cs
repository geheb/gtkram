using GtKram.Infrastructure.Database.Models;
using System.Data.Common;

namespace GtKram.Infrastructure.Database.Repositories;

internal sealed class SqlTransaction<TEntity> : ISqlTransaction where TEntity : class, IEntity
{
    private readonly SqlRepository<TEntity> _repository;
    private readonly DbTransaction _transaction;

    public SqlTransaction(SqlRepository<TEntity> repository, DbTransaction transaction)
    {
        _repository = repository;
        _transaction = transaction;
        _repository.UseTransaction(transaction);
    }

    public Task Commit(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        _repository.UseTransaction(null);
        await _transaction.DisposeAsync();
    }
}
