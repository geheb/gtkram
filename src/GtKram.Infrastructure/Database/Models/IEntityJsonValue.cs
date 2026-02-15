namespace GtKram.Infrastructure.Database.Models;

public interface IEntityJsonValue<T>
{
    T Json { get; set; }
}