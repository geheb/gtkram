using ErrorOr;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class Articles : IArticles
{
    private readonly ISqlRepository<Article, ArticleValues> _repository;

    public Articles(
        ISqlRepository<Article, ArticleValues> repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Create(Domain.Models.Article model, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object?>
        {
            ["@seller_id"] = model.SellerId,
            ["@key"] = BitConverter.ToInt64(model.SellerId.ToByteArray(), 8)
        };

        const string query = $"""
            SELECT pg_advisory_xact_lock(@key);

            INSERT INTO $table ($insert_fields)
            VALUES (
                $insert_params_without_data, 
                $data_param || jsonb_build_object('{nameof(ArticleValues.LabelNumber)}', (
                    SELECT COALESCE(MAX(($data_field->>'{nameof(ArticleValues.LabelNumber)}')::int), 0) + 1 FROM $table
                    WHERE ($data_field->>'{nameof(ArticleValues.SellerId)}')::uuid = @seller_id
                ))
            );
            """;

        var entity = model.MapToEntity(new());

        await using var trans = await _repository.BeginTransaction(cancellationToken);

        await _repository.ExecuteScalar(entity, query, values, cancellationToken);

        await trans.Commit(cancellationToken);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> Create(Domain.Models.Article[] models, Guid sellerId, CancellationToken cancellationToken)
    {
        if (models.Length == 0)
        {
            return Domain.Errors.SellerArticle.Empty;
        }

        var values = new Dictionary<string, object?>
        {
            ["@seller_id"] = sellerId,
            ["@key"] = BitConverter.ToInt64(sellerId.ToByteArray(), 8)
        };

        const string query = $"""
            SELECT pg_advisory_xact_lock(@key);

            INSERT INTO $table ($insert_fields)
            VALUES (
                $insert_params_without_data, 
                $data_param || jsonb_build_object('{nameof(ArticleValues.LabelNumber)}', (
                    SELECT COALESCE(MAX(($data_field->>'{nameof(ArticleValues.LabelNumber)}')::int), 0) + 1 FROM $table
                    WHERE ($data_field->>'{nameof(ArticleValues.SellerId)}')::uuid = @seller_id
                ))
            );
            """;

        await using var trans = await _repository.BeginTransaction(cancellationToken);

        foreach (var model in models)
        {
            var entity = model.MapToEntity(new());
            entity.Value.SellerId = sellerId;

            await _repository.ExecuteScalar(entity, query, values, cancellationToken);
        }

        await trans.Commit(cancellationToken);

        return Result.Success;

    }

    public async Task<ErrorOr<Success>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existentArticle = await _repository.SelectOne(id, cancellationToken);
        if (existentArticle is null)
        {
            return Domain.Errors.SellerArticle.NotFound;
        }

        var values = new Dictionary<string, object?>
        {
            ["@key"] = BitConverter.ToInt64(existentArticle.Value.SellerId.ToByteArray(), 8),
            ["@id"] = id,
        };

        const string query = $$"""
            SELECT pg_advisory_xact_lock(@key);

            WITH deleted AS (
                DELETE FROM $table WHERE $id_field = @id
                RETURNING 
                    $data_field->>'{{nameof(ArticleValues.SellerId)}}' AS seller_id,
                    ($data_field->>'{{nameof(ArticleValues.LabelNumber)}}')::int AS label_number
            )
            UPDATE $table
            SET $data_field = jsonb_set(
                $data_field,
                '{{{nameof(ArticleValues.LabelNumber)}}}',
                to_jsonb(($data_field->>'{{nameof(ArticleValues.LabelNumber)}}')::int - 1)
            ), $set_version_updated
            FROM deleted
            WHERE 
                $data_field->>'{{nameof(ArticleValues.SellerId)}}' = seller_id AND
                ($data_field->>'{{nameof(ArticleValues.LabelNumber)}}')::int > label_number;
            """;

        await using var trans = await _repository.BeginTransaction(cancellationToken);

        await _repository.ExecuteScalar(query, values, cancellationToken);

        await trans.Commit(cancellationToken);

        return Result.Success;

    }

    public async Task<ErrorOr<Domain.Models.Article>> Find(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);

        if (entity is null)
        {
            return Domain.Errors.SellerArticle.NotFound;
        }

        return entity.MapToDomain();
    }

    public async Task<ErrorOr<Domain.Models.Article>> FindBySellerIdAndLabelNumber(Guid sellerId, int labelNumber, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, object?>
        {
            ["@seller_id"] = sellerId,
            ["@label_number"] = labelNumber
        };

        const string query = $"""
            ($data_field->>'{nameof(ArticleValues.SellerId)}')::uuid = @seller_id AND
            ($data_field->>'{nameof(ArticleValues.LabelNumber)}')::int = @label_number
            """;

        var entities = await _repository.SelectBy(0, query, values, cancellationToken);
        var entity = entities.FirstOrDefault();
        if (entity is null)
        {
            return Domain.Errors.SellerArticle.NotFound;
        }

        return entity.MapToDomain();
    }

    public async Task<Domain.Models.Article[]> GetBySellerId(Guid id, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(ArticleValues.SellerId)}')::uuid = @id
            """;

        var entities = await _repository.SelectBy(0, query, new { id }, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<Domain.Models.Article[]> GetBySellerId(Guid[] ids, CancellationToken cancellationToken)
    {
        var result = new List<Domain.Models.Article>(ids.Length);

        const string query = $"""
            ($data_field->>'{nameof(ArticleValues.SellerId)}')::uuid = ANY(@seller_ids)
            """;

        foreach (var chunk in ids.Chunk(100))
        {
            var entities = await _repository.SelectBy(0, query, new { seller_ids = chunk }, cancellationToken);
            result.AddRange(entities.Select(e => e.MapToDomain()));
        }

        return [.. result];
    }

    public async Task<Domain.Models.Article[]> GetById(ICollection<Guid> ids, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectMany(ids, cancellationToken);
        if (entities.Length == 0)
        {
            return [];
        }

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<ErrorOr<int>> GetCountBySellerId(Guid id, CancellationToken cancellationToken)
    {
        const string query = $"""
            ($data_field->>'{nameof(ArticleValues.SellerId)}')::uuid = @seller_id
            """;

        var count = await _repository.CountBy(query, new { seller_id = id }, cancellationToken);
        return  count;
    }

    public async Task<ErrorOr<Success>> Update(Domain.Models.Article model, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(model.Id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.SellerArticle.NotFound;
        }

        model.MapToEntity(entity);
        var result = await _repository.Update(entity, cancellationToken);

        return result ? Result.Success : Domain.Errors.Internal.ConflictData;
    }
}
