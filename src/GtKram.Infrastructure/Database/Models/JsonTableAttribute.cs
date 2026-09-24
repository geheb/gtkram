using System.ComponentModel.DataAnnotations.Schema;

namespace GtKram.Infrastructure.Database.Models;

internal sealed class JsonTableAttribute : TableAttribute
{
    public JsonTableAttribute(string name, string schema) : base(name)
    {
        Schema = schema;
    }
}
