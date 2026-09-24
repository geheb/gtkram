namespace GtKram.Infrastructure.Database.Models;

using GtKram.Infrastructure.Database.Repositories;

[JsonTable(TableNames.Identities, TableSchemas.Infra)]
internal sealed class Identity : JsonEntity<IdentityValues>
{
}
