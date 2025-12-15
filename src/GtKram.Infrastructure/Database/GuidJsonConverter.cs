using System.Text.Json;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Database;

internal sealed class GuidJsonConverter : JsonConverter<Guid>
{
    public override bool HandleNull => true;

    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("Invalid Guid value");
        }
        return value.FromChar32();
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        var stringValue = value.ToChar32();
        writer.WriteStringValue(stringValue);
    }
}
