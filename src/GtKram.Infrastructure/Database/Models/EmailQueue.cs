using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.EmailQueues, TableSchemas.Infra)]
internal sealed class EmailQueue : JsonEntity<EmailQueueValues>
{
}
