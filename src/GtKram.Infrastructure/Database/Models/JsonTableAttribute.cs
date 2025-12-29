using System.ComponentModel.DataAnnotations.Schema;

namespace GtKram.Infrastructure.Database.Models;

internal sealed class JsonTableAttribute : TableAttribute
{
    public string[]? MapColumns { get; set; }

    public JsonTableAttribute(string name) : base(name)
    {
    }
}
