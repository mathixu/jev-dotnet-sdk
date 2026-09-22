using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jev;

/// <summary>State and named questions for a System One evaluation.</summary>
[JsonConverter(typeof(SystemOneRequestJsonConverter))]
public sealed class SystemOneRequest
{
    /// <summary>Creates an evaluation request.</summary>
    public SystemOneRequest(
        JevValue state,
        IReadOnlyDictionary<string, Question> questions,
        string? model = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        State = state;
        Questions = Jev.Questions.Create(questions);
        Model = model;
    }

    /// <summary>The text or structured content all questions refer to.</summary>
    public JevValue State { get; }

    /// <summary>Questions keyed by application-defined names.</summary>
    public IReadOnlyDictionary<string, Question> Questions { get; }

    /// <summary>An optional model override.</summary>
    public string? Model { get; }

    internal SystemOneRequest WithModel(string model) => new(State, Questions, model);
}

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
