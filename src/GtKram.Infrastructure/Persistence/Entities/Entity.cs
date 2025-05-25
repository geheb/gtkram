namespace GtKram.Infrastructure.Persistence.Entities;

internal record struct Entity<T>(Guid Id, DateTimeOffset Created, DateTimeOffset? Modified, T Item) where T : IEntity;
