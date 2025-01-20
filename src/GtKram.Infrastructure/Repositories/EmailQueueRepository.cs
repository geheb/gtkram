using FluentResults;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Persistence.Entities;

namespace GtKram.Infrastructure.Repositories;

internal sealed class EmailQueueRepository
{
    private readonly UuidPkGenerator _pkGenerator = new();
    private readonly AppDbContext _dbContext;

    public EmailQueueRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Create(EmailQueue entity, CancellationToken cancellationToken)
    {
        entity.Id = _pkGenerator.Generate();

        var dbSet = _dbContext.Set<EmailQueue>();

        await dbSet.AddAsync(entity, cancellationToken);

        if (await _dbContext.SaveChangesAsync(cancellationToken) < 1)
        {
            return Result.Fail("E-Mail konnte nicht gespeichert werden.");
        }

        return Result.Ok();
    }
}
