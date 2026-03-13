using System.Text.Json;
using System.Text.Json.Serialization;

namespace RipsValidatorApi.Models;

/// <summary>
/// Convierte valores JSON que pueden ser string o número hacia string.
/// Necesario porque el formato RIPS de SaludSystem mezcla tipos (ej: codServicio: 129 vs "129").
/// </summary>
public class StringOrNumberConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(),
            JsonTokenType.Null => null,
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            _ => reader.GetString()
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}
