using System.Net;

namespace Jev.Tests;

public sealed class ReliabilityTests
{
    private const string SuccessBody = """
        {"model":"jev-1.13.0","answers":{"q":{"type":"noul","noul":0.8}},"usage":{"input_tokens":1,"output_tokens":1}}
        """;

    private static readonly IReadOnlyDictionary<string, Question> OneQuestion =
        new Dictionary<string, Question> { ["q"] = Question.Noul("Is this true?") };

    [Fact]
    public async Task Retries_a_rate_limit_then_returns_the_successful_response()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.TooManyRequests, "{\"error\":{\"message\":\"slow down\"}}", ("retry-after-ms", "0"));
        handler.Enqueue(HttpStatusCode.OK, SuccessBody);
        using var client = CreateClient(handler, RetryPolicy.Default with
        {
            BackoffInitial = TimeSpan.Zero,
            BackoffJitter = 0,
        });

        var response = await client.SystemOneAsync("state", OneQuestion);

        Assert.Equal(0.8, response.GetNoul("q").Noul);
        Assert.Equal(2, handler.Requests.Count);
        Assert.False(handler.Requests[0].Headers.ContainsKey("X-TypeSafe-Retry-Count"));
        Assert.Equal("1", handler.Requests[1].Headers["X-TypeSafe-Retry-Count"]);
    }

    [Fact]
    public async Task Does_not_retry_a_bad_request()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, "{\"detail\":\"bad payload\"}", ("x-typesafe-request-id", "req_bad"));
        using var client = CreateClient(handler, RetryPolicy.Default with { BackoffInitial = TimeSpan.Zero });

        var error = await Assert.ThrowsAsync<TypeSafeBadRequestException>(() =>
            client.SystemOneAsync("state", OneQuestion));

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Equal("bad payload", error.Detail);
        Assert.Equal("req_bad", error.RequestId);
        Assert.Contains("POST https://api.typesafe.ai/v1/systemone", error.Message, StringComparison.Ordinal);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(401, typeof(TypeSafeAuthenticationException))]
    [InlineData(403, typeof(TypeSafePermissionDeniedException))]
    [InlineData(404, typeof(TypeSafeNotFoundException))]
    [InlineData(422, typeof(TypeSafeUnprocessableEntityException))]
    [InlineData(429, typeof(TypeSafeRateLimitException))]
    [InlineData(500, typeof(TypeSafeInternalServerException))]
    [InlineData(529, typeof(TypeSafeInternalServerException))]
    public async Task Maps_status_codes_to_specific_exceptions(int status, Type expectedType)
    {
        var handler = new RecordingHandler();
        handler.Enqueue((HttpStatusCode)status, "{\"message\":\"failed\"}");
        using var client = CreateClient(handler, RetryPolicy.NoRetries);

        var error = await Record.ExceptionAsync(() => client.SystemOneAsync("state", OneQuestion));

        Assert.IsType(expectedType, error);
    }

    [Fact]
    public async Task Retries_connection_failures()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(_ => throw new HttpRequestException("socket closed"));
        handler.Enqueue(HttpStatusCode.OK, SuccessBody);
        using var client = CreateClient(handler, RetryPolicy.Default with
        {
            BackoffInitial = TimeSpan.Zero,
            BackoffJitter = 0,
        });

        await client.SystemOneAsync("state", OneQuestion);

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Exhausted_connection_failure_preserves_the_cause()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(_ => throw new HttpRequestException("socket closed"));
        using var client = CreateClient(handler, RetryPolicy.NoRetries);

        var error = await Assert.ThrowsAsync<TypeSafeConnectionException>(() =>
            client.SystemOneAsync("state", OneQuestion));

        Assert.IsType<HttpRequestException>(error.InnerException);
        Assert.DoesNotContain("test-key", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Per_attempt_timeout_uses_a_specific_exception()
    {
        var handler = new RecordingHandler();
        handler.EnqueueAsync(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = CreateClient(handler, RetryPolicy.NoRetries);

        var error = await Assert.ThrowsAsync<TypeSafeTimeoutException>(() =>
            client.SystemOneAsync(
                "state",
                OneQuestion,
                options: new RequestOptions { Timeout = TimeSpan.FromMilliseconds(20) }));

        Assert.Equal(TimeSpan.FromMilliseconds(20), error.Timeout);
    }

    [Fact]
    public async Task Caller_cancellation_is_never_retried_or_wrapped()
    {
        var handler = new RecordingHandler();
        handler.EnqueueAsync(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = CreateClient(handler, RetryPolicy.Default with { BackoffInitial = TimeSpan.Zero });
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.SystemOneAsync("state", OneQuestion, cancellationToken: cancellation.Token));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Per_call_policy_can_disable_client_retries()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.ServiceUnavailable, "unavailable");
        using var client = CreateClient(handler, RetryPolicy.Default with { BackoffInitial = TimeSpan.Zero });

        await Assert.ThrowsAsync<TypeSafeInternalServerException>(() =>
            client.SystemOneAsync(
                "state",
                OneQuestion,
                options: new RequestOptions { Retry = RetryPolicy.NoRetries }));

        Assert.Single(handler.Requests);
    }

    private static TypeSafeClient CreateClient(RecordingHandler handler, RetryPolicy retry) =>
        new(new HttpClient(handler), new TypeSafeClientOptions
        {
            ApiKey = "test-key",
            Retry = retry,
        });
}
