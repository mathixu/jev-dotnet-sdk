using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jev;

internal sealed class AnswerJsonConverter : JsonConverter<Answer>
{
    public override Answer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return ResponseJson.ReadAnswer(document.RootElement, "answer");
    }

    public override void Write(Utf8JsonWriter writer, Answer value, JsonSerializerOptions options) =>
        ResponseJson.WriteAnswer(writer, value);
}
