using GtKram.Application.UseCases.Bazaar.Models;

namespace GtKram.Application.Repositories;

public interface IBazaarEvents
{
    Task<BazaarEventDto?> Find(Guid id, CancellationToken cancellationToken);
    Task<BazaarEventDto[]> GetAll(CancellationToken cancellationToken);
    Task<BazaarEventDto[]> GetAll(Guid userId, CancellationToken cancellationToken);
    Task<bool> Delete(Guid id, CancellationToken cancellationToken);
    Task<bool> Create(BazaarEventDto dto, CancellationToken cancellationToken);
    Task<bool> Update(BazaarEventDto dto, CancellationToken cancellationToken);
}
