using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Checkouts, TableSchemas.Events)]
internal sealed class Checkout : JsonEntity<CheckoutValues>
{
}
