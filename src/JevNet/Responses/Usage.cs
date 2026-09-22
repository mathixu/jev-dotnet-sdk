namespace Jev;

/// <summary>Token usage reported for an evaluation.</summary>
public sealed class Usage
{
    /// <summary>Creates token usage metadata.</summary>
    public Usage(int inputTokens, int outputTokens)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }

    /// <summary>Number of input tokens.</summary>
    public int InputTokens { get; }

    /// <summary>Number of output tokens.</summary>
    public int OutputTokens { get; }

    /// <summary>Total reported tokens.</summary>
    public int TotalTokens => checked(InputTokens + OutputTokens);
}
