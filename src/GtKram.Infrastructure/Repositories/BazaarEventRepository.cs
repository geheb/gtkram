using FluentResults;
using GtKram.Application.Converter;
using GtKram.Domain.Models;
using GtKram.Domain.Repositories;
using GtKram.Infrastructure.Persistence;
using GtKram.Infrastructure.Repositories.Mappings;
using Microsoft.EntityFrameworkCore;

namespace GtKram.Infrastructure.Repositories;

internal sealed class BazaarEventRepository : IBazaarEventRepository
{
    private const string _notFound = "Der Kinderbasar wurde nicht gefunden.";
    private readonly AppDbContext _dbContext;

    public BazaarEventRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<BazaarEvent>> Find(Guid id, CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.Set<Persistence.Entities.BazaarEvent>();

        var entity = await dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Fail(_notFound);
        }

        return entity.MapToDomain(new());
    }
}
