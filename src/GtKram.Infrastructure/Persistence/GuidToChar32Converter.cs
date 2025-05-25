using System.Text.Json;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence;

internal sealed class GuidToChar32Converter : JsonConverter<Guid>
{
    public override bool HandleNull => true;

    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("Invalid Guid value");
        }
        return Guid.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        var stringValue = value.ToString("N");
        writer.WriteStringValue(stringValue);
    }
}
