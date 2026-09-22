using System.Collections.ObjectModel;

namespace Jev;

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
