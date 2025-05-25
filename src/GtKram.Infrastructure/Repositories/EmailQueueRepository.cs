using GtKram.Domain.Base;
using GtKram.Infrastructure.Persistence.Entities;

namespace GtKram.Infrastructure.Repositories;

internal sealed class EmailQueueRepository
{
    private readonly TimeProvider _timeProvider;
    private readonly IRepository<EmailQueue> _repo;

    public EmailQueueRepository(
        TimeProvider timeProvider,
        IRepository<EmailQueue> repo)
    {
        _timeProvider = timeProvider;
        _repo = repo;
    }

    public async Task<Result> Create(EmailQueue entity, CancellationToken cancellationToken)
    {
        await _repo.Create(entity, null, cancellationToken);
        return Result.Ok();
    }

    public async Task<EmailQueue[]> GetBySentIsNull(CancellationToken cancellationToken)
    {
        var entities = await _repo.Query(
            [new(static e => e.Sent, null)],
            null,
            cancellationToken);

        return [.. entities.Select(e => e.Item)];
    }

    public async Task<Result> UpdateSent(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repo.Find(id, null, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.Internal.EmailNotFound);
        }

        entity.Value.Item.Sent = _timeProvider.GetUtcNow();

        var result = await _repo.Update(entity.Value.Item, null, cancellationToken);

        if (result != UpdateResult.Success)
        {
            return Result.Fail(Domain.Errors.Internal.EmailSaveFailed);
        }

        return Result.Ok();
    }
}
