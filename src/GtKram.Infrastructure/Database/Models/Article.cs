using GtKram.Infrastructure.Database.Repositories;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Articles, TableSchemas.Events)]
internal sealed class Article : JsonEntity<ArticleValues>
{
}
