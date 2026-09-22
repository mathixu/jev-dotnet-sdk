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

internal static class ResponseJson
{
    internal static Answer ReadAnswer(JsonElement element, string path)
    {
        var answer = RequireObject(element, path);
        var type = RequireString(answer, "type", $"{path}.type");
        return type switch
        {
            "noul" => new NoulAnswer(RequireDouble(answer, "noul", $"{path}.noul")),
            "choice" => new ChoiceAnswer(
                RequireString(answer, "choice", $"{path}.choice"),
                RequireDouble(answer, "confidence", $"{path}.confidence"),
                ReadDoubleMap(answer, "probabilities", $"{path}.probabilities")),
            "score" => new ScoreAnswer(
                RequireDouble(answer, "score", $"{path}.score"),
                RequireDouble(answer, "confidence", $"{path}.confidence"),
                ReadValueMap(answer, "legend", $"{path}.legend"),
                ReadDoubleMap(answer, "probabilities", $"{path}.probabilities")),
            _ => throw Invalid($"{path}.type"),
        };
    }

    internal static void WriteAnswer(Utf8JsonWriter writer, Answer value)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        switch (value)
        {
            case NoulAnswer noul:
                writer.WriteNumber("noul", noul.Noul);
                break;
            case ChoiceAnswer choice:
                writer.WriteString("choice", choice.Choice);
                writer.WriteNumber("confidence", choice.Confidence);
                WriteDoubleMap(writer, "probabilities", choice.Probabilities);
                break;
            case ScoreAnswer score:
                writer.WriteNumber("score", score.Score);
                writer.WriteNumber("confidence", score.Confidence);
                writer.WritePropertyName("legend");
                writer.WriteStartObject();
                foreach (var entry in score.Legend)
                {
                    writer.WritePropertyName(entry.Key);
                    entry.Value.WriteTo(writer);
                }

                writer.WriteEndObject();
                WriteDoubleMap(writer, "probabilities", score.Probabilities);
                break;
            default:
                throw new JsonException($"Unsupported answer type '{value.GetType().Name}'.");
        }

        writer.WriteEndObject();
    }

    internal static JsonElement RequireProperty(JsonElement parent, string name, string path)
    {
        if (!parent.TryGetProperty(name, out var value))
        {
            throw Invalid(path);
        }

        return value;
    }

    internal static JsonElement RequireObject(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw Invalid(path);
        }

        return value;
    }

    internal static string RequireString(JsonElement parent, string name, string path)
    {
        var value = RequireProperty(parent, name, path);
        if (value.ValueKind != JsonValueKind.String)
        {
            throw Invalid(path);
        }

        return value.GetString()!;
    }

    internal static int RequireInt32(JsonElement parent, string name, string path)
    {
        var value = RequireProperty(parent, name, path);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
        {
            throw Invalid(path);
        }

        return result;
    }

    internal static double RequireDouble(JsonElement parent, string name, string path)
    {
        var value = RequireProperty(parent, name, path);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var result))
        {
            throw Invalid(path);
        }

        return result;
    }

    internal static JsonException Invalid(string path) =>
        new($"Invalid response data at '{path}'.");

    private static Dictionary<string, double> ReadDoubleMap(
        JsonElement parent,
        string name,
        string path)
    {
        var element = RequireObject(RequireProperty(parent, name, path), path);
        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Number ||
                !property.Value.TryGetDouble(out var number))
            {
                throw Invalid($"{path}.{property.Name}");
            }

            result[property.Name] = number;
        }

        return result;
    }

    private static Dictionary<string, JevValue> ReadValueMap(
        JsonElement parent,
        string name,
        string path)
    {
        var element = RequireObject(RequireProperty(parent, name, path), path);
        var result = new Dictionary<string, JevValue>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            try
            {
                result[property.Name] = JevValue.FromJson(property.Value.GetRawText());
            }
            catch (ArgumentException error)
            {
                throw Invalid($"{path}.{property.Name}", error);
            }
        }

        return result;
    }

    private static void WriteDoubleMap(
        Utf8JsonWriter writer,
        string name,
        IReadOnlyDictionary<string, double> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        foreach (var entry in values)
        {
            writer.WriteNumber(entry.Key, entry.Value);
        }

        writer.WriteEndObject();
    }

    private static JsonException Invalid(string path, Exception innerException) =>
        new($"Invalid response data at '{path}'.", innerException);
}
