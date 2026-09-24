using ErrorOr;
using GtKram.Application.Converter;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Sellers : ISellers
{
    private readonly ISqlRepository<Seller, SellerValues> _repository;

    public Sellers(
        ISqlRepository<Seller, SellerValues> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Guid>> Create(Domain.Models.Seller model, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object?>
        {
            ["@event_id"] = model.EventId,
            ["@key"] = BitConverter.ToInt64(model.EventId.ToByteArray(), 8),
        };

        var entity = model.MapToEntity(new());
        entity.Id = Guid.CreateVersion7();
        entity.Value.IdentityId = model.IdentityId;

        await using var trans = await _repository.BeginTransaction(cancellationToken);

        if (model.SellerNumber == 0)
        {
            // assign next value

            const string query = $"""
                SELECT pg_advisory_xact_lock(@key);

                INSERT INTO $table ($insert_fields)
                VALUES (
                    $insert_params_without_data, 
                    $data_param || jsonb_build_object('{nameof(SellerValues.SellerNumber)}', (
                        SELECT COALESCE(MAX(($data_field->>'{nameof(SellerValues.SellerNumber)}')::int), 0) + 1 FROM $table
                        WHERE ($data_field->>'{nameof(SellerValues.EventId)}')::uuid = @event_id
                    ))
                );
                """;

            await _repository.ExecuteScalar(entity, query, values, cancellationToken);
        }
        else 
        {
            // assign provided value, but correct others

            values["@seller_number"] = model.SellerNumber;

            const string query = $$"""
                SELECT pg_advisory_xact_lock(@key);

                UPDATE $table
                SET $data_field = jsonb_set(
                    $data_field,
                    '{{{nameof(SellerValues.SellerNumber)}}}',
                    to_jsonb(($data_field->>'{{nameof(SellerValues.SellerNumber)}}')::int + 1)
                )
                WHERE 
                    ($data_field->>'{{nameof(SellerValues.EventId)}}')::uuid = @event_id AND
                    ($data_field->>'{{nameof(SellerValues.SellerNumber)}}')::int >= @seller_number;                    

                $insert_table_query
                """;

            await _repository.ExecuteScalar(entity, query, values, cancellationToken);
        }

        await trans.Commit(cancellationToken);

        return entity.Id;
    }

    public async Task<ErrorOr<Domain.Models.Seller>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);

        if (entity is null)
        {
            return Domain.Errors.Seller.NotFound;
        }

        return entity.MapToDomain(new());
    }

    public async Task<Domain.Models.Seller[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(SellerValues.EventId)}')::uuid = @event_id
            """;

        var entities = await _repository.SelectBy(0, query, new { event_id = id }, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<Domain.Models.Seller[]> GetByIdentityId(Guid id, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(SellerValues.IdentityId)}')::uuid = @identity_id
            """;

        var entities = await _repository.SelectBy(0, query, new { identity_id = id }, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        return entities.Select(e => e.MapToDomain(dc)).ToArray();
    }

    public async Task<ErrorOr<Domain.Models.Seller>> FindByIdentityIdAndEventId(Guid identityId, Guid eventId, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(SellerValues.IdentityId)}')::uuid = @identity_id AND
            ($data_field->>'{nameof(SellerValues.EventId)}')::uuid = @event_id
            """;

        var entities = await _repository.SelectBy(0, query, new { identity_id = identityId, event_id = eventId }, cancellationToken);
        var entity = entities.FirstOrDefault();
        if (entity is null)
        {
            return Domain.Errors.Seller.NotFound;
        }

        return entity.MapToDomain(new());
    }

    public async Task<ErrorOr<Success>> Update(Domain.Models.Seller model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Seller.NotFound;
        }

        var values = new Dictionary<string, object?>
        {
            ["@event_id"] = model.EventId,
            ["@key"] = BitConverter.ToInt64(model.EventId.ToByteArray(), 8),
            ["@new_number"] = model.SellerNumber,
            ["@old_number"] = entity.Value.SellerNumber
        };

        const string query = $$"""
            SELECT pg_advisory_xact_lock(@key);

            UPDATE $table
            SET $data_field = jsonb_set(
                $data_field,
                '{{{nameof(SellerValues.SellerNumber)}}}',
                to_jsonb(
                    CASE
                        WHEN @new_number > @old_number
                            AND ($data_field->>'{{nameof(SellerValues.SellerNumber)}}')::int > @old_number
                            AND ($data_field->>'{{nameof(SellerValues.SellerNumber)}}')::int <= @new_number
                            THEN ($data_field->>'{{nameof(SellerValues.SellerNumber)}}')::int - 1
                        WHEN @new_number < @old_number
                            AND ($data_field->>'{{nameof(SellerValues.SellerNumber)}}')::int >= @new_number
                            AND ($data_field->>'{{nameof(SellerValues.SellerNumber)}}')::int < @old_number
                            THEN ($data_field->>'{{nameof(SellerValues.SellerNumber)}}')::int + 1
                        ELSE ($data_field->>'{{nameof(SellerValues.SellerNumber)}}')::int
                    END
                )
            )
            WHERE 
                ($data_field->>'{{nameof(SellerValues.EventId)}}')::uuid = @event_id AND
                $id_field <> $id_param;

            $update_table_query
            """;

        model.MapToEntity(entity);

        await using var trans = await _repository.BeginTransaction(cancellationToken);

        await _repository.ExecuteScalar(entity, query, values, cancellationToken);

        await trans.Commit(cancellationToken);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var affectedRows = await _repository.Delete(id, cancellationToken);

        return affectedRows > 0 ? Result.Success : Domain.Errors.Seller.NotFound;
    }

    public async Task<Domain.Models.Seller[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectMany(ids, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();
        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<ErrorOr<Domain.Models.Seller>> FindByEventIdAndSellerNumber(Guid eventId, int sellerNumber, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(SellerValues.EventId)}')::uuid = @event_id AND
            ($data_field->>'{nameof(SellerValues.SellerNumber)}')::int = @seller_number
            """;

        var entities = await _repository.SelectBy(0, query, new { event_id = eventId, seller_number = sellerNumber }, cancellationToken);
        var entity = entities.FirstOrDefault();

        if (entity is null)
        {
            return Domain.Errors.Seller.NotFound;
        }

        return entity.MapToDomain(new());
    }
}
