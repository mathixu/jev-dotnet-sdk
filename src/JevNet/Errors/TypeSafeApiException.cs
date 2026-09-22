using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;

namespace Jev;

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
