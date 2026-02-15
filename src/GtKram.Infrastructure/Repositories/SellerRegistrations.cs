using ErrorOr;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class SellerRegistrations : ISellerRegistrations
{
    private readonly TableLocker _tableLocker;
    private readonly ISqlRepository<SellerRegistration, SellerRegistrationValues> _repository;

    public SellerRegistrations(
        TableLocker tableLocker,
        ISqlRepository<SellerRegistration, SellerRegistrationValues> repository)
    {
        _tableLocker = tableLocker;
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Create(Domain.Models.SellerRegistration model, CancellationToken cancellationToken)
    {
        using var locker = await _tableLocker.LockSellerRegistration(cancellationToken);
        if (locker is null)
        {
            return Domain.Errors.SellerRegistration.Timeout;
        }

        var entity = model.MapToEntity(new() { Json = new() });
        await _repository.Insert(entity, cancellationToken);
        return Result.Success;
    }

    public async Task<ErrorOr<Domain.Models.SellerRegistration>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);

        if (entity is null)
        {
            return Domain.Errors.SellerRegistration.NotFound;
        }

        return entity.MapToDomain();
    }

    public async Task<ErrorOr<Domain.Models.SellerRegistration>> FindBySellerId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.SellerId, id, cancellationToken);

        if (entities.Length == 0)
        {
            return Domain.Errors.SellerRegistration.NotFound;
        }

        return entities[0].MapToDomain();
    }

    public async Task<ErrorOr<Domain.Models.SellerRegistration>> FindByEventIdAndEmail(Guid eventId, string email, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, eventId, cancellationToken);
        var entity = entities.FirstOrDefault(e => e.Json.Email!.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (entity is null)
        {
            return Domain.Errors.SellerRegistration.NotFound;
        }

        return entity.MapToDomain();
    }

    public async Task<Domain.Models.SellerRegistration[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAll(cancellationToken);

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetAllByAccepted(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAll(cancellationToken);

        return [.. entities.Where(e => e.Json.IsAccepted == true).Select(e => e.MapToDomain())];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, id, cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken)
    {
        var result = new List<Domain.Models.SellerRegistration>(ids.Length);

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _repository.SelectBy(0, e => e.SellerId, chunk, cancellationToken);
            result.AddRange(entities.Select(e => e.MapToDomain()));
        }

        return [.. result];
    }

    public async Task<ErrorOr<Success>> Update(Domain.Models.SellerRegistration model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.SellerRegistration.NotFound;
        }

        model.MapToEntity(entity);
        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Domain.Errors.Internal.ConflictData;
    }

    public async Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _repository.Delete(id, cancellationToken);
        return result > 0 ? Result.Success : Domain.Errors.SellerRegistration.NotFound;
    }

    public async Task<ErrorOr<int>> GetCountByEventId(Guid id, CancellationToken cancellationToken)
    {
        using var locker = await _tableLocker.LockSellerRegistration(cancellationToken);
        if (locker is null)
        {
            return Domain.Errors.SellerRegistration.Timeout;
        }

        var count = await _repository.CountBy(e => e.EventId, id, cancellationToken);
        return count;
    }
}
