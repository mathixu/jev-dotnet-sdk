namespace Jev;

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
