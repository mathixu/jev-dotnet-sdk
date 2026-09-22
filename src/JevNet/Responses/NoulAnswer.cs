namespace Jev;

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
