using ErrorOr;
using GtKram.Application.Converter;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Checkouts : ICheckouts
{
    private readonly ISqlRepository<Checkout, CheckoutValues> _repository;
    private readonly string _articleTable;
    public Checkouts(
        ISqlRepository<Checkout, CheckoutValues> repository,
        ISqlRepository<Article, ArticleValues> articleRepo)
    {
        _repository = repository;
        _articleTable = articleRepo.Table;
    }

    public async Task<ErrorOr<Guid>> Create(Guid eventId, Guid identityId, CancellationToken cancellationToken)
    {
        var entity = new Checkout
        {
            Value = new()
            {
                EventId = eventId,
                IdentityId = identityId,
                Status = (int)Domain.Models.CheckoutStatus.InProgress
            }
        };

        var id = await _repository.Insert(entity, cancellationToken);

        return id;
    }

    public async Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var affectedRows = await _repository.Delete(id, cancellationToken);

        return affectedRows > 0 ? Result.Success : Domain.Errors.Checkout.NotFound;
    }

    public async Task<ErrorOr<Domain.Models.Checkout>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Checkout.NotFound;
        }

        return entity.MapToDomain(new());
    }

    public async Task<Domain.Models.Checkout[]> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAll(cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetByEventId(Guid id, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(CheckoutValues.EventId)}')::uuid = @event_id
            """;

        var entities = await _repository.SelectBy(0, query, new { event_id = id }, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetByEventIdAndUserId(Guid eventId, Guid identityId, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(CheckoutValues.EventId)}')::uuid = @event_id AND
            ($data_field->>'{nameof(CheckoutValues.IdentityId)}')::uuid = @identity_id
            """;

        var entities = await _repository.SelectBy(0, query, new { event_id = eventId, identity_id = identityId }, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetById(Guid[] ids, CancellationToken cancellationToken)
    {
        var dc = new GermanDateTimeConverter();

        var entities = await _repository.SelectMany(ids, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<Domain.Models.Checkout[]> GetByIdentityId(Guid id, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(CheckoutValues.IdentityId)}')::uuid = @identity_id
            """;

        var entities = await _repository.SelectBy(0, query, new { identity_id = id }, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        var dc = new GermanDateTimeConverter();

        return [.. entities.Select(e => e.MapToDomain(dc))];
    }

    public async Task<bool> HasArticle(Guid eventId, Guid articleId, CancellationToken cancellationToken)
    {
        const string query = $"""
            $data_field @> jsonb_build_object(
                '{nameof(CheckoutValues.EventId)}', @event_id,
                '{nameof(CheckoutValues.ArticleIds)}', jsonb_build_array(@article_id)
            )
            """;

        var count = await _repository.CountBy(query, new { event_id = eventId, article_id = articleId }, cancellationToken);
        return count > 0;
    }

    public async Task<ErrorOr<Success>> SetCompleted(Guid id, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object?>
        {
            ["@id"] = id,
            ["@new_status"] = (int)Domain.Models.CheckoutStatus.Completed,
            ["@old_status"] = (int)Domain.Models.CheckoutStatus.InProgress
        };

        const string query = $$"""
            UPDATE $table 
            SET $data_field = jsonb_set(
                $data_field,
                '{{{nameof(CheckoutValues.Status)}}}',
                to_jsonb(@new_status)
            ), $set_version_updated
            WHERE $id_field = @id 
                AND ($data_field->>'{{nameof(CheckoutValues.Status)}}')::int = @old_status
                AND jsonb_array_length($data_field->'{{nameof(CheckoutValues.ArticleIds)}}') > 0
            RETURNING 1;
            """;

        var result = await _repository.ExecuteScalar(query, values, cancellationToken);
        return result is not null ? Result.Success : Domain.Errors.Checkout.CompleteFailed;
    }

    public async Task<ErrorOr<Success>> AddArticle(Guid checkoutId, Guid articleId, Guid eventId, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object?>
        {
            ["@checkout_id"] = checkoutId,
            ["@article_id"] = articleId,
            ["@event_id"] = eventId,
        };

        var query = $$"""
            WITH existing_array AS (
                SELECT $id_field id, $data_field data
                FROM $table
                WHERE $id_field = @checkout_id AND NOT EXISTS (
                    SELECT 1 FROM $table WHERE 
                    ($data_field->>'{{nameof(CheckoutValues.EventId)}}')::uuid = @event_id AND
                    ($data_field->'{{nameof(CheckoutValues.ArticleIds)}}' @> jsonb_build_array(@article_id::text))
                )
                FOR UPDATE
            ),
            updated_array AS (
                SELECT id, (data->'{{nameof(CheckoutValues.ArticleIds)}}') || to_jsonb(@article_id::text) new_items
                FROM existing_array
            ),
            calculated_total AS (
                SELECT 
                    ua.id,
                    ua.new_items,
                    COALESCE(SUM((a.$data_field->>'{{nameof(ArticleValues.Price)}}')::int), 0) new_total
                FROM updated_array ua
                JOIN LATERAL jsonb_array_elements_text(ua.new_items) item_id_text ON TRUE
                JOIN {{_articleTable}} a ON a.$id_field = item_id_text::uuid
                GROUP BY ua.id, ua.new_items
            )
            UPDATE $table o
            SET $data_field = jsonb_set(
                jsonb_set(o.$data_field, '{{{nameof(CheckoutValues.ArticleIds)}}}', ct.new_items),
                '{{{nameof(CheckoutValues.Total)}}}', 
                to_jsonb(ct.new_total)
            ), $set_version_updated
            FROM calculated_total ct
            WHERE o.$id_field = ct.id
            RETURNING 1;
            """;

        var result = await _repository.ExecuteScalar(query, values, cancellationToken);
        return result is not null ? Result.Success : Domain.Errors.Checkout.AddArticleFailed;
    }

    public async Task<ErrorOr<Success>> DeleteArticle(Guid checkoutId, Guid articleId, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object?>
        {
            ["@checkout_id"] = checkoutId,
            ["@article_id"] = articleId
        };

        var query = $$"""
            WITH updated_array AS (
                SELECT 
                    $id_field id,
                    ($data_field->'{{nameof(CheckoutValues.ArticleIds)}}') - @article_id::text new_items
                FROM $table
                WHERE $id_field = @checkout_id AND 
                    $data_field->'{{nameof(CheckoutValues.ArticleIds)}}' @> jsonb_build_array(@article_id::text)
                FOR UPDATE
            ),
            calculated_total AS (
                SELECT 
                    ua.id,
                    ua.new_items,
                    COALESCE(SUM((a.$data_field->>'{{nameof(ArticleValues.Price)}}')::int), 0) new_total
                FROM updated_array ua
                JOIN LATERAL jsonb_array_elements_text(ua.new_items) item_id_text ON TRUE
                JOIN {{_articleTable}} a ON a.$id_field = item_id_text::uuid
                GROUP BY ua.id, ua.new_items
            )
            UPDATE $table o
            SET $data_field = jsonb_set(
                jsonb_set(o.$data_field, '{{{nameof(CheckoutValues.ArticleIds)}}}', ct.new_items),
                '{{{nameof(CheckoutValues.Total)}}}', 
                to_jsonb(ct.new_total)
            ), $set_version_updated
            FROM calculated_total ct
            WHERE o.$id_field = ct.id
            RETURNING 1;
            """;

        var result = await _repository.ExecuteScalar(query, values, cancellationToken);
        return result is not null ? Result.Success : Domain.Errors.Checkout.DeleteArticleFailed;
    }

    public async Task<ErrorOr<Success>> Update(Domain.Models.Checkout model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Checkout.NotFound;
        }

        model.MapToEntity(entity);
        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Domain.Errors.Internal.ConflictData;
    }
}
