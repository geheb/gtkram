using GtKram.Infrastructure.Database.Repositories;
using System.Text.Json;

namespace GtKram.Infrastructure.Database.Models;

[JsonTable(TableNames.Articles, MapColumns = [nameof(SellerId), nameof(LabelNumber)])]
internal sealed class Article : IEntity, IEntityJsonValue<ArticleValues>
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Updated { get; set; }

    public string JsonProperties { get; set; } = null!;

    public int JsonVersion { get; set; }

    public Guid SellerId => Json.SellerId;

    public int LabelNumber => Json.LabelNumber;

    public ArticleValues Json { get; set; } = null!;
}
