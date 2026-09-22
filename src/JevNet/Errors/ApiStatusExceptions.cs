using System.Net;
using System.Text.Json;

namespace Jev;

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
        : base(status, body, headers, endpoint) => RetryAfter = RetryAfterParser.Parse(headers);

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
