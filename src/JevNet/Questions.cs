using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jev;

/// <summary>A typed question evaluated against shared state.</summary>
[JsonConverter(typeof(QuestionJsonConverter))]
public abstract class Question
{
    private protected Question()
    {
    }

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

/// <summary>Optional descriptions of the true and false outcomes of a noul.</summary>
public sealed class NoulCriteria
{
    /// <summary>Creates outcome descriptions.</summary>
    public NoulCriteria(JevValue? @true = null, JevValue? @false = null)
    {
        True = @true;
        False = @false;
    }

    /// <summary>What counts as true.</summary>
    public JevValue? True { get; }

    /// <summary>What counts as false.</summary>
    public JevValue? False { get; }
}

/// <summary>A yes/no question whose answer is the probability of true.</summary>
public sealed class NoulQuestion : Question
{
    internal NoulQuestion(JevValue? instructions, NoulCriteria? criteria)
    {
        Instructions = instructions;
        Criteria = criteria;
    }

    /// <inheritdoc />
    public override string Type => "noul";

    /// <summary>Optional descriptions of both outcomes.</summary>
    public NoulCriteria? Criteria { get; }
}

/// <summary>A question that selects one named alternative.</summary>
public sealed class ChoiceQuestion : Question
{
    internal ChoiceQuestion(JevValue? instructions, IReadOnlyDictionary<string, JevValue?> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        Instructions = instructions;
        Criteria = new ReadOnlyDictionary<string, JevValue?>(
            new Dictionary<string, JevValue?>(criteria, StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public override string Type => "choice";

    /// <summary>Labels mapped to optional descriptions.</summary>
    public IReadOnlyDictionary<string, JevValue?> Criteria { get; }
}

/// <summary>A question that assigns an expected score against an ordered rubric.</summary>
public sealed class ScoreQuestion : Question
{
    internal ScoreQuestion(JevValue? instructions, IReadOnlyList<JevValue> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        if (criteria.Count < 2)
        {
            throw new ArgumentException("A score question requires at least two levels.", nameof(criteria));
        }

        if (criteria.Any(static level => level is null))
        {
            throw new ArgumentException("Score levels cannot be null.", nameof(criteria));
        }

        Instructions = instructions;
        Criteria = Array.AsReadOnly(criteria.ToArray());
    }

    /// <inheritdoc />
    public override string Type => "score";

    /// <summary>Ordered score descriptions indexed from zero.</summary>
    public IReadOnlyList<JevValue> Criteria { get; }
}

/// <summary>Utilities for constructing named question sets.</summary>
public static class Questions
{
    /// <summary>Creates an immutable snapshot and rejects an empty request.</summary>
    public static IReadOnlyDictionary<string, Question> Create(
        IReadOnlyDictionary<string, Question> questions)
    {
        ArgumentNullException.ThrowIfNull(questions);
        if (questions.Count == 0)
        {
            throw new ArgumentException("At least one question is required.", nameof(questions));
        }

        var snapshot = new Dictionary<string, Question>(questions.Count, StringComparer.Ordinal);
        foreach (var pair in questions)
        {
            if (pair.Value is null)
            {
                throw new ArgumentException($"Question '{pair.Key}' cannot be null.", nameof(questions));
            }

            snapshot.Add(pair.Key, pair.Value);
        }

        return new ReadOnlyDictionary<string, Question>(snapshot);
    }
}

internal sealed class QuestionJsonConverter : JsonConverter<Question>
{
    public override Question Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("Question deserialization is not part of the public wire contract.");

    public override void Write(Utf8JsonWriter writer, Question value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        writer.WritePropertyName("instructions");
        WriteValue(writer, value.Instructions);

        switch (value)
        {
            case NoulQuestion { Criteria: not null } noul:
                writer.WritePropertyName("criteria");
                writer.WriteStartObject();
                writer.WritePropertyName("true");
                WriteValue(writer, noul.Criteria.True);
                writer.WritePropertyName("false");
                WriteValue(writer, noul.Criteria.False);
                writer.WriteEndObject();
                break;
            case ChoiceQuestion choice:
                writer.WritePropertyName("criteria");
                writer.WriteStartObject();
                foreach (var pair in choice.Criteria)
                {
                    writer.WritePropertyName(pair.Key);
                    WriteValue(writer, pair.Value);
                }

                writer.WriteEndObject();
                break;
            case ScoreQuestion score:
                writer.WritePropertyName("criteria");
                writer.WriteStartArray();
                foreach (var level in score.Criteria)
                {
                    level.WriteTo(writer);
                }

                writer.WriteEndArray();
                break;
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, JevValue? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            value.WriteTo(writer);
        }
    }
}
