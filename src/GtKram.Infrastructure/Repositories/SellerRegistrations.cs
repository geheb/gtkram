using ErrorOr;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Identity;

namespace GtKram.Infrastructure.Repositories;

internal sealed class SellerRegistrations : ISellerRegistrations
{
    private readonly ILookupNormalizer _lookupNormalizer;
    private readonly ISqlRepository<SellerRegistration, SellerRegistrationValues> _repository;
    public readonly string _eventTable;

    public SellerRegistrations(
        ILookupNormalizer lookupNormalizer,
        ISqlRepository<SellerRegistration, SellerRegistrationValues> repository,
        ISqlRepository<Event, EventValues> eventRepo)
    {
        _lookupNormalizer = lookupNormalizer;
        _repository = repository;
        _eventTable = eventRepo.Table;
    }

    public async Task<ErrorOr<Guid>> Upsert(Domain.Models.SellerRegistration model, CancellationToken cancellationToken)
    {
        var entity = model.MapToEntity(new(), _lookupNormalizer);

        var values = new Dictionary<string, object?>
        {
            ["@event_id"] = entity.Value.EventId,
            ["@normalized_email"] = entity.Value.NormalizedEmail,
            ["@name"] = entity.Value.Name,
            ["@phone"] = entity.Value.Phone,
            ["@clothing"] = entity.Value.Clothing,
            ["@preferred_type"] = entity.Value.PreferredType
        };

        string query = $$"""
            WITH locked_event AS (
                SELECT ($data_field->>'{{nameof(EventValues.MaxSellers)}}')::int AS max_sellers
                FROM {{_eventTable}}
                WHERE $id_field = @event_id
                FOR UPDATE
            )
            INSERT INTO $table AS r ($insert_fields)
            SELECT $insert_params
            FROM locked_event
            WHERE (
                SELECT COUNT(*) FROM $table WHERE ($data_field->>'{{nameof(SellerRegistrationValues.EventId)}}')::uuid = @event_id
            ) < max_sellers
            ON CONFLICT (
                ($data_field->>'{{nameof(SellerRegistrationValues.EventId)}}'),
                ($data_field->>'{{nameof(SellerRegistrationValues.NormalizedEmail)}}')
            ) 
            WHERE 
                ($data_field->>'{{nameof(SellerRegistrationValues.EventId)}}') IS NOT NULL AND
                ($data_field->>'{{nameof(SellerRegistrationValues.NormalizedEmail)}}') IS NOT NULL
            DO UPDATE SET
                $data_field = r.$data_field || jsonb_build_object(
                    '{{nameof(SellerRegistrationValues.Name)}}', @name,
                    '{{nameof(SellerRegistrationValues.Phone)}}', @phone,
                    '{{nameof(SellerRegistrationValues.Clothing)}}', @clothing::text,
                    '{{nameof(SellerRegistrationValues.PreferredType)}}', @preferred_type
                ), $version_field=r.$version_field+1, $updated_field=EXCLUDED.$created_field
            RETURNING $id_field;
            """;

        var id = (Guid?)await _repository.ExecuteScalar(entity, query, values, cancellationToken);

        return id is null ? Domain.Errors.Internal.ConflictData : id.Value;
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
        const string query = $"""
            ($data_field->>'{nameof(SellerRegistrationValues.SellerId)}')::uuid = @seller_id
            """;

        var entities = await _repository.SelectBy(0, query, new { seller_id = id }, cancellationToken);

        if (entities.Length == 0)
        {
            return Domain.Errors.SellerRegistration.NotFound;
        }

        return entities[0].MapToDomain();
    }

    public async Task<ErrorOr<Domain.Models.SellerRegistration>> FindByEventIdAndEmail(Guid eventId, string email, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(SellerRegistrationValues.EventId)}')::uuid = @event_id AND
            $data_field->>'{nameof(SellerRegistrationValues.NormalizedEmail)}' = @normalized_email
            """;

        var normalizedEmail = _lookupNormalizer.NormalizeEmail(email);

        var entities = await _repository.SelectBy(0, query, new { event_id = eventId, normalized_email = normalizedEmail }, cancellationToken);
        var entity = entities.FirstOrDefault();
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
        const string query = $"""
            $data_field->>'{nameof(SellerRegistrationValues.IsAccepted)}' IS TRUE
            """;

        var entities = await _repository.SelectBy(0, query, null, cancellationToken);

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(SellerRegistrationValues.EventId)}')::uuid = @event_id
            """;

        var entities = await _repository.SelectBy(0, query, new { event_id = id }, cancellationToken);

        if (entities.Length == 0)
        {
            return [];
        }

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<Domain.Models.SellerRegistration[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken)
    {
        var result = new List<Domain.Models.SellerRegistration>(ids.Length);

        const string query = $"""
            ($data_field->>'{nameof(SellerRegistrationValues.SellerId)}')::uuid = ANY(@seller_ids)
            """;

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _repository.SelectBy(0, query, new { seller_ids = chunk }, cancellationToken);
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

        model.MapToEntity(entity, _lookupNormalizer);
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
        const string query = $"""
            ($data_field->>'{nameof(SellerRegistrationValues.EventId)}')::uuid = @event_id
            """;

        var count = await _repository.CountBy(query, new { event_id = id }, cancellationToken);
        return count;
    }
}
