using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jev;

internal sealed class SystemOneResponseJsonConverter : JsonConverter<SystemOneResponse>
{
    public override SystemOneResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = ResponseJson.RequireObject(document.RootElement, "$response");
        var model = ResponseJson.RequireString(root, "model", "model");
        var answerElement = ResponseJson.RequireObject(
            ResponseJson.RequireProperty(root, "answers", "answers"),
            "answers");

        var answers = new Dictionary<string, Answer>(StringComparer.Ordinal);
        foreach (var property in answerElement.EnumerateObject())
        {
            answers[property.Name] = ResponseJson.ReadAnswer(
                property.Value,
                $"answers.{property.Name}");
        }

        if (answers.Count == 0)
        {
            throw ResponseJson.Invalid("answers");
        }

        var usageElement = ResponseJson.RequireObject(
            ResponseJson.RequireProperty(root, "usage", "usage"),
            "usage");
        var usage = new Usage(
            ResponseJson.RequireInt32(usageElement, "input_tokens", "usage.input_tokens"),
            ResponseJson.RequireInt32(usageElement, "output_tokens", "usage.output_tokens"));

        return new SystemOneResponse(model, answers, usage);
    }

    public override void Write(Utf8JsonWriter writer, SystemOneResponse value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("model", value.Model);
        writer.WritePropertyName("answers");
        writer.WriteStartObject();
        foreach (var answer in value.Answers)
        {
            writer.WritePropertyName(answer.Key);
            ResponseJson.WriteAnswer(writer, answer.Value);
        }

        writer.WriteEndObject();
        writer.WritePropertyName("usage");
        writer.WriteStartObject();
        writer.WriteNumber("input_tokens", value.Usage.InputTokens);
        writer.WriteNumber("output_tokens", value.Usage.OutputTokens);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}
