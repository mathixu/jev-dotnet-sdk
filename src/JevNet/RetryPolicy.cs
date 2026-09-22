using System.Collections.Frozen;

namespace Jev;

/// <summary>Controls retry eligibility and exponential backoff.</summary>
public sealed record RetryPolicy
{
    private static readonly IReadOnlySet<int> DefaultStatuses =
        new[] { 408, 429 }.Concat(Enumerable.Range(500, 100)).ToFrozenSet();

    /// <summary>The SDK default retry policy.</summary>
    public static RetryPolicy Default { get; } = new();

    /// <summary>A policy that performs a single attempt.</summary>
    public static RetryPolicy NoRetries { get; } = new() { MaxRetries = 0 };

    /// <summary>Maximum retries after the initial attempt.</summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>Delay before the first retry.</summary>
    public TimeSpan BackoffInitial { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Maximum client-computed backoff.</summary>
    public TimeSpan BackoffMaximum { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Fraction randomly subtracted from each backoff, from zero to one.</summary>
    public double BackoffJitter { get; init; } = 0.25;

    /// <summary>HTTP status codes eligible for retry.</summary>
    public IReadOnlySet<int> StatusCodes { get; init; } = DefaultStatuses;

    /// <summary>Whether connection failures are retried.</summary>
    public bool RetryConnectionErrors { get; init; } = true;

    /// <summary>Whether per-attempt timeouts are retried.</summary>
    public bool RetryTimeouts { get; init; } = true;

    /// <summary>Whether valid server retry headers take precedence over backoff.</summary>
    public bool RespectRetryAfter { get; init; } = true;

    /// <summary>Largest server-requested delay the client will honor.</summary>
    public TimeSpan MaximumRetryAfter { get; init; } = TimeSpan.FromMinutes(1);

    internal RetryPolicy Validate()
    {
        if (MaxRetries < 0)
        {
            throw new TypeSafeConfigurationException("RetryPolicy.MaxRetries cannot be negative.");
        }

        ValidateDuration(BackoffInitial, nameof(BackoffInitial));
        ValidateDuration(BackoffMaximum, nameof(BackoffMaximum));
        ValidateDuration(MaximumRetryAfter, nameof(MaximumRetryAfter));
        if (!double.IsFinite(BackoffJitter) || BackoffJitter is < 0 or > 1)
        {
            throw new TypeSafeConfigurationException("RetryPolicy.BackoffJitter must be between zero and one.");
        }

        ArgumentNullException.ThrowIfNull(StatusCodes);
        if (StatusCodes.Any(status => status is < 100 or > 999))
        {
            throw new TypeSafeConfigurationException("RetryPolicy.StatusCodes contains an invalid HTTP status.");
        }

        return this with { StatusCodes = StatusCodes.ToFrozenSet() };
    }

    private static void ValidateDuration(TimeSpan value, string name)
    {
        if (value < TimeSpan.Zero || value == Timeout.InfiniteTimeSpan)
        {
            throw new TypeSafeConfigurationException($"RetryPolicy.{name} cannot be negative or infinite.");
        }
    }
}
