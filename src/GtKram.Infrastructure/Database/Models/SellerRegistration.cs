using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.SellerRegistrations, TableSchemas.Events)]
internal sealed class SellerRegistration : JsonEntity<SellerRegistrationValues>
{
}
