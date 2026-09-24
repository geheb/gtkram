using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Events, TableSchemas.Events)]
internal sealed class Event : JsonEntity<EventValues>
{
}
