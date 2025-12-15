namespace GtKram.Infrastructure.Database.Entities;

internal interface IEntity
{
    Guid Id { get; set; }
    int Version { get; set; }
    DateTimeOffset? Created { get; set; }
}
