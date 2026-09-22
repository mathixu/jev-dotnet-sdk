using System.Collections.ObjectModel;

namespace Jev;

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
