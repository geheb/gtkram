using ErrorOr;
using GtKram.Infrastructure.Database.Models;
using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Repositories;

internal sealed class EmailQueues
{
    private readonly TimeProvider _timeProvider;
    private readonly ISqlRepository<EmailQueue, EmailQueueValues> _repository;

    public EmailQueues(
        TimeProvider timeProvider,
        ISqlRepository<EmailQueue, EmailQueueValues> repository)
    {
        _timeProvider = timeProvider;
        _repository = repository;
    }

    public async Task<ErrorOr<Success>> Create(Domain.Models.EmailQueue model, CancellationToken cancellationToken)
    {
        var entity = new EmailQueue
        {
            Value = new()
            {
                Recipient = model.Recipient,
                Subject = model.Subject,
                Body = model.Body,
                AttachmentName = model.AttachmentName,
                AttachmentMimeType = model.AttachmentMimeType,
                AttachmentBlob = model.AttachmentBlob,
            },
        };
        await _repository.Insert(entity, cancellationToken);
        return Result.Success;
    }

    public async Task<Domain.Models.EmailQueue[]> GetNotSent(int count, CancellationToken cancellationToken)
    {
        const string query = $"""
            $data_field->>'{nameof(EmailQueueValues.Sent)}' IS NULL
            """;

        var entities = await _repository.SelectBy(count, query, null, cancellationToken);

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<ErrorOr<Success>> UpdateSent(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Internal.EmailNotFound;
        }

        var values = new Dictionary<string, object?>
        {
            ["@sent"] = _timeProvider.GetUtcNow(),
            ["@id"] = id
        };

        const string query = $"""
            UPDATE $table SET $data_field->>'{nameof(EmailQueueValues.Sent)}' = @sent 
            WHERE $id_field = @id
            RETURNING 1
            """;

        var result = await _repository.ExecuteScalar(query, values, cancellationToken);

        if (result is null)
        {
            return Domain.Errors.Internal.ConflictData;
        }

        return Result.Success;
    }
}
