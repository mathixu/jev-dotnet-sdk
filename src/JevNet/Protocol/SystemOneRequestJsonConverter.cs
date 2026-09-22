using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jev;

internal sealed class SystemOneRequestJsonConverter : JsonConverter<SystemOneRequest>
{
    public override SystemOneRequest Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        throw new NotSupportedException("SystemOneRequest deserialization is not supported.");

    public override void Write(
        Utf8JsonWriter writer,
        SystemOneRequest value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("state");
        value.State.WriteTo(writer);
        writer.WritePropertyName("questions");
        writer.WriteStartObject();
        foreach (var question in value.Questions)
        {
            writer.WritePropertyName(question.Key);
            JsonSerializer.Serialize<Question>(writer, question.Value, options);
        }

        writer.WriteEndObject();
        if (value.Model is not null)
        {
            writer.WriteString("model", value.Model);
        }

        writer.WriteEndObject();
    }
}
