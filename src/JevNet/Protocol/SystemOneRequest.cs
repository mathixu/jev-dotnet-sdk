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
