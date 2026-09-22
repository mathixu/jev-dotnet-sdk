namespace Jev;

using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;

/// <summary>Base class for failures reported by the SDK.</summary>
public class TypeSafeException : Exception
{
    /// <summary>Creates an SDK exception.</summary>
    public TypeSafeException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an SDK exception with its underlying cause.</summary>
    public TypeSafeException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Invalid or incomplete client configuration.</summary>
public sealed class TypeSafeConfigurationException : TypeSafeException
{
    /// <summary>Creates a configuration exception.</summary>
    public TypeSafeConfigurationException(string message)
        : base(message)
    {
    }
}

/// <summary>An unsuccessful HTTP response returned by the TypeSafe API.</summary>
public class TypeSafeApiException : TypeSafeException
{
    internal TypeSafeApiException(
        HttpStatusCode statusCode,
        string? responseBody,
        IReadOnlyDictionary<string, string> headers,
        string endpoint,
        string? detail = null,
        Exception? innerException = null)
        : base(CreateMessage(statusCode, endpoint, detail ?? ExtractDetail(responseBody), headers),
            innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
        Headers = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase));
        Endpoint = endpoint;
        Detail = detail ?? ExtractDetail(responseBody);
        RequestId = Headers.TryGetValue("x-typesafe-request-id", out var requestId) ? requestId : null;
    }

    /// <summary>The HTTP status code.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The unmodified response body, when present.</summary>
    public string? ResponseBody { get; }

    /// <summary>A read-only snapshot of the response headers.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>The request method and URL, without credentials or query parameters.</summary>
    public string Endpoint { get; }

    /// <summary>The most useful human-readable detail extracted from the response.</summary>
    public string? Detail { get; }

    /// <summary>The server request identifier, when supplied.</summary>
    public string? RequestId { get; }

    private static string CreateMessage(
        HttpStatusCode status,
        string endpoint,
        string? detail,
        IReadOnlyDictionary<string, string> headers)
    {
        var message = $"{endpoint}: {(int)status}";
        if (!string.IsNullOrEmpty(detail))
        {
            message += $" {detail}";
        }

        if (headers.TryGetValue("x-typesafe-request-id", out var requestId))
        {
            message += $" (request_id={requestId})";
        }

        return message;
    }

    private static string? ExtractDetail(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                return root.GetString();
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                return body;
            }

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString();
                }

                if (error.ValueKind == JsonValueKind.Object &&
                    error.TryGetProperty("message", out var errorMessage) &&
                    errorMessage.ValueKind == JsonValueKind.String)
                {
                    return errorMessage.GetString();
                }
            }

            foreach (var name in new[] { "message", "detail" })
            {
                if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // The plain response remains the most useful diagnostic.
        }

        return body.Length <= 1_000 ? body : $"{body[..1_000]}…";
    }
}

/// <summary>The request was invalid (HTTP 400).</summary>
public sealed class TypeSafeBadRequestException : TypeSafeApiException
{
    internal TypeSafeBadRequestException(HttpStatusCode status, string? body, IReadOnlyDictionary<string, string> headers, string endpoint)
        : base(status, body, headers, endpoint) { }
}

/// <summary>Authentication failed (HTTP 401).</summary>
public sealed class TypeSafeAuthenticationException : TypeSafeApiException
{
    internal TypeSafeAuthenticationException(HttpStatusCode status, string? body, IReadOnlyDictionary<string, string> headers, string endpoint)
        : base(status, body, headers, endpoint) { }
}

/// <summary>Access was denied (HTTP 403).</summary>
public sealed class TypeSafePermissionDeniedException : TypeSafeApiException
{
    internal TypeSafePermissionDeniedException(HttpStatusCode status, string? body, IReadOnlyDictionary<string, string> headers, string endpoint)
        : base(status, body, headers, endpoint) { }
}

/// <summary>The resource was not found (HTTP 404).</summary>
public sealed class TypeSafeNotFoundException : TypeSafeApiException
{
    internal TypeSafeNotFoundException(HttpStatusCode status, string? body, IReadOnlyDictionary<string, string> headers, string endpoint)
        : base(status, body, headers, endpoint) { }
}

/// <summary>The request failed server validation (HTTP 422).</summary>
public sealed class TypeSafeUnprocessableEntityException : TypeSafeApiException
{
    internal TypeSafeUnprocessableEntityException(HttpStatusCode status, string? body, IReadOnlyDictionary<string, string> headers, string endpoint)
        : base(status, body, headers, endpoint) { }
}

/// <summary>The API rate limit was exceeded (HTTP 429).</summary>
public sealed class TypeSafeRateLimitException : TypeSafeApiException
{
    internal TypeSafeRateLimitException(HttpStatusCode status, string? body, IReadOnlyDictionary<string, string> headers, string endpoint)
        : base(status, body, headers, endpoint)
    {
        RetryAfter = RetryAfterParser.Parse(headers);
    }

    /// <summary>The server-requested delay, when valid.</summary>
    public TimeSpan? RetryAfter { get; }
}

/// <summary>The TypeSafe service failed to process the request (HTTP 5xx).</summary>
public sealed class TypeSafeInternalServerException : TypeSafeApiException
{
    internal TypeSafeInternalServerException(HttpStatusCode status, string? body, IReadOnlyDictionary<string, string> headers, string endpoint)
        : base(status, body, headers, endpoint) { }
}

/// <summary>A successful HTTP response did not match the documented schema.</summary>
public sealed class TypeSafeResponseValidationException : TypeSafeApiException
{
    internal TypeSafeResponseValidationException(HttpStatusCode status, string? body, IReadOnlyDictionary<string, string> headers, string endpoint, JsonException cause)
        : base(status, body, headers, endpoint, cause.Message, cause) { }
}

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

internal static class TypeSafeApiExceptionFactory
{
    internal static TypeSafeApiException Create(
        HttpStatusCode status,
        string? body,
        IReadOnlyDictionary<string, string> headers,
        string endpoint) => (int)status switch
        {
            400 => new TypeSafeBadRequestException(status, body, headers, endpoint),
            401 => new TypeSafeAuthenticationException(status, body, headers, endpoint),
            403 => new TypeSafePermissionDeniedException(status, body, headers, endpoint),
            404 => new TypeSafeNotFoundException(status, body, headers, endpoint),
            422 => new TypeSafeUnprocessableEntityException(status, body, headers, endpoint),
            429 => new TypeSafeRateLimitException(status, body, headers, endpoint),
            >= 500 => new TypeSafeInternalServerException(status, body, headers, endpoint),
            _ => new TypeSafeApiException(status, body, headers, endpoint),
        };
}

internal static class RetryAfterParser
{
    internal static TimeSpan? Parse(IReadOnlyDictionary<string, string> headers)
    {
        if (headers.TryGetValue("retry-after-ms", out var milliseconds) &&
            double.TryParse(milliseconds, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var millisecondValue) &&
            double.IsFinite(millisecondValue) && millisecondValue >= 0)
        {
            return TimeSpan.FromMilliseconds(millisecondValue);
        }

        if (!headers.TryGetValue("retry-after", out var retryAfter))
        {
            return null;
        }

        if (double.TryParse(retryAfter, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var seconds) &&
            double.IsFinite(seconds) && seconds >= 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (DateTimeOffset.TryParse(retryAfter, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var date))
        {
            return date <= DateTimeOffset.UtcNow ? TimeSpan.Zero : date - DateTimeOffset.UtcNow;
        }

        return null;
    }
}
