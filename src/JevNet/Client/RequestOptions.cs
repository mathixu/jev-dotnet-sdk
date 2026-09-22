namespace Jev;

/// <summary>Options that apply to one API call.</summary>
public sealed class RequestOptions
{
    /// <summary>Overrides the per-attempt client timeout.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Additional headers. Per-call values replace client defaults case-insensitively.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>Overrides the client's retry policy for this call.</summary>
    public RetryPolicy? Retry { get; init; }
}
