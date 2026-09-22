using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jev;

/// <summary>A typed answer returned by Jev.</summary>
[JsonConverter(typeof(AnswerJsonConverter))]
public abstract class Answer
{
    private protected Answer()
    {
    }

    /// <summary>The wire discriminator for this answer.</summary>
    public abstract string Type { get; }
}

/// <summary>A yes/no answer expressed as the probability of true.</summary>
public sealed class NoulAnswer : Answer
{
    /// <summary>Creates a noul answer.</summary>
    public NoulAnswer(double noul) => Noul = noul;

    /// <inheritdoc />
    public override string Type => "noul";

    /// <summary>The probability of true, from zero to one.</summary>
    public double Noul { get; }
}

/// <summary>A selected label and the distribution over all labels.</summary>
public sealed class ChoiceAnswer : Answer
{
    /// <summary>Creates a choice answer.</summary>
    public ChoiceAnswer(
        string choice,
        double confidence,
        IReadOnlyDictionary<string, double> probabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(choice);
        ArgumentNullException.ThrowIfNull(probabilities);
        Choice = choice;
        Confidence = confidence;
        Probabilities = Snapshot(probabilities);
    }

    /// <inheritdoc />
    public override string Type => "choice";

    /// <summary>The label with the highest probability.</summary>
    public string Choice { get; }

    /// <summary>The model's confidence in the selection.</summary>
    public double Confidence { get; }

    /// <summary>Probabilities keyed by label.</summary>
    public IReadOnlyDictionary<string, double> Probabilities { get; }

    private static ReadOnlyDictionary<string, double> Snapshot(
        IReadOnlyDictionary<string, double> values) =>
        new(new Dictionary<string, double>(values, StringComparer.Ordinal));
}

/// <summary>An expected score and the distribution over its rubric levels.</summary>
public sealed class ScoreAnswer : Answer
{
    /// <summary>Creates a score answer.</summary>
    public ScoreAnswer(
        double score,
        double confidence,
        IReadOnlyDictionary<string, JevValue> legend,
        IReadOnlyDictionary<string, double> probabilities)
    {
        ArgumentNullException.ThrowIfNull(legend);
        ArgumentNullException.ThrowIfNull(probabilities);
        Score = score;
        Confidence = confidence;
        Legend = new ReadOnlyDictionary<string, JevValue>(
            new Dictionary<string, JevValue>(legend, StringComparer.Ordinal));
        Probabilities = new ReadOnlyDictionary<string, double>(
            new Dictionary<string, double>(probabilities, StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public override string Type => "score";

    /// <summary>The probability-weighted expected score.</summary>
    public double Score { get; }

    /// <summary>The model's confidence in the score.</summary>
    public double Confidence { get; }

    /// <summary>The requested rubric keyed by zero-based score.</summary>
    public IReadOnlyDictionary<string, JevValue> Legend { get; }

    /// <summary>Probabilities keyed by score.</summary>
    public IReadOnlyDictionary<string, double> Probabilities { get; }
}
