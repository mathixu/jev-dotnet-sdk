using System.Collections.Concurrent;
using System.Net;

namespace Jev.Tests;

internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    public void Enqueue(HttpStatusCode statusCode, string body, params (string Name, string Value)[] headers)
    {
        _responses.Enqueue((_, _) =>
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            };
            foreach (var (name, value) in headers)
            {
                response.Headers.TryAddWithoutValidation(name, value);
            }

            return Task.FromResult(response);
        });
    }

    public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        _responses.Enqueue((request, _) => Task.FromResult(response(request)));

    public void EnqueueAsync(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) =>
        _responses.Enqueue(response);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.AsEnumerable();
        if (request.Content is not null)
        {
            headers = headers.Concat(request.Content.Headers);
        }

        Requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri!,
            headers.ToDictionary(
                header => header.Key,
                header => string.Join(",", header.Value),
                StringComparer.OrdinalIgnoreCase),
            body));

        if (!_responses.TryDequeue(out var response))
        {
            throw new InvalidOperationException("No HTTP response was queued for this request.");
        }

        return await response(request, cancellationToken);
    }
}

internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    IReadOnlyDictionary<string, string> Headers,
    string? Body);
