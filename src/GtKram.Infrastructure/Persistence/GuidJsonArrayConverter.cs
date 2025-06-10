using System.Text.Json;
using System.Text.Json.Serialization;

namespace GtKram.Infrastructure.Persistence;

internal sealed class GuidJsonArrayConverter : JsonConverter<IEnumerable<Guid>>
{
    public override IEnumerable<Guid> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Unexpected token");
        }

        var result = new List<Guid>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return result;
            }
            
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Unexpected token");
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException($"Empty value");
            }

            result.Add(value.FromChar32());
        }

        throw new JsonException("Invalid content"); 
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<Guid> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var v in value)
        {
            writer.WriteStringValue(v.ToChar32());
        }
        writer.WriteEndArray();
    }
}
