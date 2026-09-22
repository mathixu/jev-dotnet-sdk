using System.Text.Json.Serialization;

namespace Jev;

/// <summary>A typed question evaluated against shared state.</summary>
[JsonConverter(typeof(QuestionJsonConverter))]
public abstract class Question
{
    private protected Question() { }

    /// <summary>Creates a yes/no probability question.</summary>
    public static NoulQuestion Noul(JevValue? instructions = null, NoulCriteria? criteria = null) =>
        new(instructions, criteria);

    /// <summary>Creates a question that selects one named alternative.</summary>
    public static ChoiceQuestion Choice(
        JevValue? instructions,
        IReadOnlyDictionary<string, JevValue?> criteria) => new(instructions, criteria);

    /// <summary>Creates a question that assigns a score against an ordered rubric.</summary>
    public static ScoreQuestion Score(JevValue? instructions, params JevValue[] criteria) =>
        new(instructions, criteria);

    /// <summary>The wire discriminator for this question.</summary>
    public abstract string Type { get; }

    /// <summary>The content describing what Jev should decide.</summary>
    public JevValue? Instructions { get; private protected init; }
}
