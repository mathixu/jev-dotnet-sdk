using System.Net;

namespace Jev;

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
