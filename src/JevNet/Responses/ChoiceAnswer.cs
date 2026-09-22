using System.Collections.ObjectModel;

namespace Jev;

/// <summary>A selected label and the distribution over all labels.</summary>
public sealed class ChoiceAnswer : Answer
{
    /// <summary>Creates a choice answer.</summary>
    public ChoiceAnswer(
        string choice,
        double confidence,
        IReadOnlyDictionary<string, double> probabilities)
    {
        ArgumentNullException.ThrowIfNull(choice);
        ArgumentNullException.ThrowIfNull(probabilities);
        Choice = choice;
        Confidence = confidence;
        Probabilities = new ReadOnlyDictionary<string, double>(
            new Dictionary<string, double>(probabilities, StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public override string Type => "choice";

    /// <summary>The label with the highest probability.</summary>
    public string Choice { get; }

    /// <summary>The model's confidence in the selection.</summary>
    public double Confidence { get; }

    /// <summary>Probabilities keyed by label.</summary>
    public IReadOnlyDictionary<string, double> Probabilities { get; }
}
