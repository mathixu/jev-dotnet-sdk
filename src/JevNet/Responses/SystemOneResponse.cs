using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jev;

/// <summary>The result of one System One evaluation.</summary>
[JsonConverter(typeof(SystemOneResponseJsonConverter))]
public sealed class SystemOneResponse
{
    /// <summary>Creates a response, primarily for tests and application fakes.</summary>
    public SystemOneResponse(
        string model,
        IReadOnlyDictionary<string, Answer> answers,
        Usage usage,
        string? requestId = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentNullException.ThrowIfNull(usage);
        if (answers.Count == 0)
        {
            throw new ArgumentException("At least one answer is required.", nameof(answers));
        }

        Model = model;
        Answers = new ReadOnlyDictionary<string, Answer>(
            new Dictionary<string, Answer>(answers, StringComparer.Ordinal));
        Usage = usage;
        RequestId = requestId;
    }

    /// <summary>The concrete model that evaluated the request.</summary>
    public string Model { get; }

    /// <summary>Answers keyed by the names supplied in the request.</summary>
    public IReadOnlyDictionary<string, Answer> Answers { get; }

    /// <summary>Token usage for the evaluation.</summary>
    public Usage Usage { get; }

    /// <summary>The server request identifier, when supplied as a response header.</summary>
    public string? RequestId { get; }

    /// <summary>Gets a noul answer by question name.</summary>
    public NoulAnswer GetNoul(string name) => Get<NoulAnswer>(name, "noul");

    /// <summary>Gets a choice answer by question name.</summary>
    public ChoiceAnswer GetChoice(string name) => Get<ChoiceAnswer>(name, "choice");

    /// <summary>Gets a score answer by question name.</summary>
    public ScoreAnswer GetScore(string name) => Get<ScoreAnswer>(name, "score");

    private TAnswer Get<TAnswer>(string name, string expectedType)
        where TAnswer : Answer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Answers.TryGetValue(name, out var answer))
        {
            throw new KeyNotFoundException($"The response does not contain an answer named '{name}'.");
        }

        if (answer is not TAnswer typed)
        {
            throw new InvalidOperationException(
                $"Answer '{name}' is of type '{answer.Type}', not '{expectedType}'.");
        }

        return typed;
    }
}
