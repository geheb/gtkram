using GtKram.Application.Converter;
using GtKram.Domain.Base;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class SellerRegistrations : ISellerRegistrations
{
    private readonly TableLocker _tableLocker;
    private readonly ISqlRepository<SellerRegistration> _repository;

    public SellerRegistrations(
        TableLocker tableLocker,
        ISqlRepository<SellerRegistration> repository)
    {
        _tableLocker = tableLocker;
        _repository = repository;
    }

    public async Task<Result> Create(Domain.Models.SellerRegistration model, CancellationToken cancellationToken)
    {
        if (!await _tableLocker.SellerRegistration.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.SellerRegistration.Timeout);
        }

        try
        {
            var entity = model.MapToEntity(new() { Json = new() }, new());
            await _repository.Insert(entity, cancellationToken);
            return Result.Ok();
        }
        finally
        {
            _tableLocker.SellerRegistration.Release();
        }
    }

    public async Task<Result<Domain.Models.SellerRegistration>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);

        if (entity is null)
        {
            return Result.Fail(Domain.Errors.SellerRegistration.NotFound);
        }

        return entity.MapToDomain(new());
    }

    public async Task<Result<Domain.Models.SellerRegistration>> FindBySellerId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.SellerId, id, cancellationToken);

        if (entities.Length == 0)
        {
            return Result.Fail(Domain.Errors.SellerRegistration.NotFound);
        }

        return entities[0].MapToDomain(new());
    }

    public async Task<Result<Domain.Models.SellerRegistration>> FindByEventIdAndEmail(Guid eventId, string email, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, eventId, cancellationToken);
        var entity = entities.FirstOrDefault(e => e.Json.Email!.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.SellerRegistration.NotFound);
        }

        return entity.MapToDomain(new());
    }

    public async Task<Domain.Models.SellerRegistration[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAll(cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetAllByAccepted(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAll(cancellationToken);

        var dc = new GermanDateTimeConverter();
        return [.. entities.Where(e => e.Json.IsAccepted == true).Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectBy(0, e => e.EventId, id, cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken)
    {
        var result = new List<Domain.Models.SellerRegistration>(ids.Length);
        var dc = new GermanDateTimeConverter();

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _repository.SelectBy(0, e => e.SellerId, chunk, cancellationToken);
            result.AddRange(entities.Select(e => e.MapToDomain(dc)));
        }

        return [.. result];
    }

    public async Task<Result> Update(Domain.Models.SellerRegistration model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(Domain.Errors.SellerRegistration.NotFound);
        }

        model.MapToEntity(entity, new());
        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Ok() : Result.Fail(Domain.Errors.SellerRegistration.SaveFailed);
    }

    public async Task<Result> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _repository.Delete(id, cancellationToken);
        return result > 0 ? Result.Ok() : Result.Fail(Domain.Errors.SellerRegistration.SaveFailed);
    }

    public async Task<Result<int>> GetCountByEventId(Guid id, CancellationToken cancellationToken)
    {
        if (!await _tableLocker.SellerRegistration.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            return Result.Fail(Domain.Errors.SellerRegistration.Timeout);
        }

        try
        {
            var count = await _repository.CountBy(e => e.EventId, id, cancellationToken);
            return Result.Ok(count);
        }
        finally
        {
            _tableLocker.SellerRegistration.Release();
        }
    }
}
