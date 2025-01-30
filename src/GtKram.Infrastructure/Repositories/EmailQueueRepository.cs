using FluentResults;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class EmailQueueRepository
{
    private const string _notFound = "Der Email wurde nicht gefunden.";
    private const string _saveFailed = "Der Email konnte nicht gespeichert werden.";
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly TimeProvider _timeProvider;
    private readonly AppDbContext _dbContext;

    public EmailQueueRepository(
        TimeProvider timeProvider,
        AppDbContext dbContext)
    {
        _timeProvider = timeProvider;
        _dbContext = dbContext;
    }

    public async Task<Result> Create(EmailQueue entity, CancellationToken cancellationToken)
    {
        entity.Id = _pkGenerator.Generate();

        var dbSet = _dbContext.Set<EmailQueue>();

        await dbSet.AddAsync(entity, cancellationToken);

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            return Result.Fail(_saveFailed);
        }

        return Result.Ok();
    }

    public async Task<EmailQueue[]> GetPending(CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<EmailQueue>();

        return await dbSet
            .AsNoTracking()
            .Take(128)
            .Where(e => e.SentOn == null)
            .OrderBy(e => e.CreatedOn)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Result> UpdateSentOn(Guid id, CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<EmailQueue>();

        var entity = await dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(_notFound);
        }

        entity.SentOn = _timeProvider.GetUtcNow();

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            return Result.Fail(_saveFailed);
        }

        return Result.Ok();
    }
}
