namespace Jev;

/// <summary>A request failed before receiving an HTTP response.</summary>
public class TypeSafeConnectionException : TypeSafeException
{
    internal TypeSafeConnectionException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>An HTTP attempt exceeded its configured timeout.</summary>
public sealed class TypeSafeTimeoutException : TypeSafeConnectionException
{
    internal TypeSafeTimeoutException(TimeSpan timeout, Exception innerException)
        : base($"Request timed out after {timeout}.", innerException) => Timeout = timeout;

    /// <summary>The timeout applied to the failed attempt.</summary>
    public TimeSpan Timeout { get; }
}
