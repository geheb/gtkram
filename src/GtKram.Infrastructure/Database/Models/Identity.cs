using GtKram.Infrastructure.Database.Repositories;
using System.Text.Json;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Identities, MapColumns = [nameof(Email)])]
internal sealed class Identity : IEntity, IEntityJsonValue<IdentityValues>
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public string JsonProperties { get; set; } = null!;

    public int JsonVersion { get; set; }

    public string Email => Json.Email;

    public IdentityValues Json { get; set; } = null!;
}
