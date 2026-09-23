namespace Jev;

/// <summary>Construction-time settings for <see cref="TypeSafeClient"/>.</summary>
public sealed class TypeSafeClientOptions
{
    /// <summary>Backend used to execute Jev decisions.</summary>
    public JevProvider Provider { get; init; } = JevProvider.TypeSafe;

    /// <summary>API key. Falls back to the selected provider's environment variable.</summary>
    public string? ApiKey { get; init; }

    /// <summary>API root. Falls back to the selected provider's environment variable and default.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>Default model. Falls back to the selected provider's environment variable and default.</summary>
    public string? DefaultModel { get; init; }

    /// <summary>Timeout for each HTTP attempt.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Headers included in every request. Authentication and content headers are protected.</summary>
    public IReadOnlyDictionary<string, string>? DefaultHeaders { get; init; }

    /// <summary>Retry behavior. Defaults to two retries for transient failures.</summary>
    public RetryPolicy Retry { get; init; } = RetryPolicy.Default;
}
