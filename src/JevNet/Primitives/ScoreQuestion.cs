namespace Jev;

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
