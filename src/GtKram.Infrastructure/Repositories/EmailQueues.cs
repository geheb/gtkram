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
            Json = new()
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
        var entities = await _repository.SelectBy(count, e => e.IsSent, false, cancellationToken);

        return [.. entities.Select(e => e.MapToDomain())];
    }

    public async Task<ErrorOr<Success>> UpdateSent(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.SelectOne(id, cancellationToken);
        if (entity is null)
        {
            return Domain.Errors.Internal.EmailNotFound;
        }

        entity.Json.Sent = _timeProvider.GetUtcNow();

        var result = await _repository.Update(entity, cancellationToken);

        if (!result)
        {
            return Domain.Errors.Internal.ConflictData;
        }

        return Result.Success;
    }
}
