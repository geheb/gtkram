using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Plannings, TableSchemas.Events)]
internal sealed class Planning : JsonEntity<PlanningValues>
{
}
