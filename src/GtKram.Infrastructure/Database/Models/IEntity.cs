namespace GtKram.Infrastructure.Database.Models;

public interface IEntity
{
    Guid Id { get; set; }
    DateTime Created {  get; set; }
    DateTime? Updated { get; set; }
    string JsonProperties { get; set; }
    int JsonVersion { get; set; }
    void Serialize();
    void Deserialize();
}
