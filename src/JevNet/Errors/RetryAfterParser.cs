using System.Globalization;

namespace Jev;

internal static class RetryAfterParser
{
    internal static TimeSpan? Parse(IReadOnlyDictionary<string, string> headers)
    {
        if (headers.TryGetValue("retry-after-ms", out var milliseconds) &&
            TryDuration(milliseconds, multiplier: 1, out var millisecondDelay))
        {
            return millisecondDelay;
        }

        if (!headers.TryGetValue("retry-after", out var retryAfter))
        {
            return null;
        }

        if (TryDuration(retryAfter, multiplier: 1_000, out var secondDelay))
        {
            return secondDelay;
        }

        if (DateTimeOffset.TryParse(
                retryAfter,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var date))
        {
            return date <= DateTimeOffset.UtcNow ? TimeSpan.Zero : date - DateTimeOffset.UtcNow;
        }

        return null;
    }

    private static bool TryDuration(string value, double multiplier, out TimeSpan delay)
    {
        delay = default;
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ||
            !double.IsFinite(number) || number < 0)
        {
            return false;
        }

        var milliseconds = number * multiplier;
        if (!double.IsFinite(milliseconds) || milliseconds > TimeSpan.MaxValue.TotalMilliseconds)
        {
            return false;
        }

        delay = TimeSpan.FromMilliseconds(milliseconds);
        return true;
    }
}
