using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Sellers, TableSchemas.Events)]
internal sealed class Seller : JsonEntity<SellerValues>
{
}
