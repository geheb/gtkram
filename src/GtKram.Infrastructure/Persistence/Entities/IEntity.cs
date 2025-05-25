namespace GtKram.Infrastructure.Persistence.Entities;

internal interface IEntity
{
    Guid Id { get; set; }
    int Version { get; set; }
}
