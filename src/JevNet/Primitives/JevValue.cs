using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Jev;

/// <summary>A JSON string, object, array, or null accepted by the Jev API.</summary>
[JsonConverter(typeof(JevValueJsonConverter))]
public sealed class JevValue
{
    private readonly JsonElement _value;

    private JevValue(JsonElement value)
    {
        EnsureSupported(value);
        _value = value.Clone();
    }

    /// <summary>Creates a value from a JSON-serializable .NET value.</summary>
    public static JevValue From<T>(T value, JsonSerializerOptions? options = null)
    {
        var element = JsonSerializer.SerializeToElement(value, options ?? JevJson.SerializerOptions);
        return new JevValue(element);
    }

    /// <summary>Creates a value with source-generated JSON metadata for trim-safe applications.</summary>
    public static JevValue From<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return new JevValue(JsonSerializer.SerializeToElement(value, typeInfo));
    }

    /// <summary>Creates a value from one complete JSON value.</summary>
    public static JevValue FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);
        return new JevValue(document.RootElement);
    }

    /// <summary>Returns an independent JSON element representing this value.</summary>
    public JsonElement ToJsonElement() => _value.Clone();

    /// <summary>Converts text to a Jev value.</summary>
    public static implicit operator JevValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return From(value);
    }

    internal void WriteTo(Utf8JsonWriter writer) => _value.WriteTo(writer);

    private static void EnsureSupported(JsonElement value)
    {
        if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Object or
            JsonValueKind.Array or JsonValueKind.Null))
        {
            throw new ArgumentException(
                "A Jev value must be a JSON string, object, array, or null.",
                nameof(value));
        }
    }
}

internal sealed class JevValueJsonConverter : JsonConverter<JevValue>
{
    public override JevValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return JevValue.FromJson(document.RootElement.GetRawText());
    }

    public override void Write(Utf8JsonWriter writer, JevValue value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.WriteTo(writer);
    }
}
