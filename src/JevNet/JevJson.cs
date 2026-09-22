using System.Text.Json;

namespace Jev;

/// <summary>JSON helpers configured for the Jev wire contract.</summary>
public static class JevJson
{
    private static readonly JsonSerializerOptions DefaultOptions = new(JsonSerializerDefaults.Web);

    internal static JsonSerializerOptions SerializerOptions => DefaultOptions;

    /// <summary>Serializes a value using the SDK's wire-format settings.</summary>
    public static string Serialize<T>(T value) => value switch
    {
        Question question => JsonSerializer.Serialize<Question>(question, DefaultOptions),
        Answer answer => JsonSerializer.Serialize<Answer>(answer, DefaultOptions),
        _ => JsonSerializer.Serialize(value, DefaultOptions),
    };

    /// <summary>Deserializes a value using the SDK's wire-format settings.</summary>
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, DefaultOptions);
}
