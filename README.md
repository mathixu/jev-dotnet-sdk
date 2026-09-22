# JevNet

An idiomatic .NET client for [TypeSafe AI](https://typesafe.ai/) and its Jev System One model.

JevNet turns text or structured application state into typed decisions: a probability (`Noul`), a selected label (`Choice`), or an expected score (`Score`). The core package targets .NET 8 or later and has no runtime package dependencies.

> [!IMPORTANT]
> JevNet is a community project. It is not affiliated with or endorsed by TypeSafe AI.

## Install

```shell
dotnet add package JevNet
```

For `IHttpClientFactory` and `Microsoft.Extensions.DependencyInjection` integration:

```shell
dotnet add package JevNet.Extensions.DependencyInjection
```

Set `TYPESAFE_API_KEY` in the process environment. Never put an API key in source control.

## Quick start

```csharp
using Jev;

using var client = new TypeSafeClient();

var result = await client.SystemOneAsync(
    new
    {
        ticket = new
        {
            subject = "Duplicate charge",
            message = "I was charged twice. Please refund the duplicate today.",
        },
    },
    new Dictionary<string, Question>
    {
        ["refund_requested"] = Question.Noul(
            "Is the customer asking for money back?"),
        ["department"] = Question.Choice(
            "Which team should handle this?",
            new Dictionary<string, JevValue?>
            {
                ["billing"] = "Charges, invoices, subscriptions, and refunds",
                ["support"] = "Product questions and technical problems",
                ["other"] = null,
            }),
        ["urgency"] = Question.Score(
            "How urgently should this be handled?",
            "Can wait",
            "Handle soon",
            "Handle now"),
    });

Console.WriteLine(result.GetNoul("refund_requested").Noul);
Console.WriteLine(result.GetChoice("department").Choice);
Console.WriteLine(result.GetScore("urgency").Score);
Console.WriteLine(result.Model);
Console.WriteLine(result.RequestId);
```

All network methods are asynchronous and accept a `CancellationToken`.

## Questions and answers

| Primitive | Builder | Answer | Typical use |
| --- | --- | --- | --- |
| Noul | `Question.Noul(...)` | `NoulAnswer.Noul` | gates, moderation, yes/no judgments |
| Choice | `Question.Choice(...)` | `ChoiceAnswer.Choice`, `Probabilities`, `Confidence` | routing and classification |
| Score | `Question.Score(...)` | `ScoreAnswer.Score`, `Legend`, `Probabilities`, `Confidence` | severity, quality, and risk |

A call evaluates all named questions against the same state. Answers keep the names supplied in the request and are available through `response.Answers` or the checked `GetNoul`, `GetChoice`, and `GetScore` accessors.

Text converts to `JevValue` implicitly. Use `JevValue.From(value)` for structured instructions or criteria. A source-generated overload is available for trim-safe applications:

```csharp
var value = JevValue.From(state, AppJsonContext.Default.TicketState);
```

## Configuration

Explicit options win over environment variables, which win over SDK defaults.

| Option | Environment variable | Default |
| --- | --- | --- |
| `ApiKey` | `TYPESAFE_API_KEY` | required |
| `BaseUrl` | `TYPESAFE_BASE_URL` | `https://api.typesafe.ai` |
| `DefaultModel` | `TYPESAFE_DEFAULT_MODEL` | `jev-latest` |
| `Timeout` | — | 10 seconds per attempt |
| `Retry` | — | 2 retries, 500–5000 ms backoff, 25% subtractive jitter |

```csharp
using var client = new TypeSafeClient(new TypeSafeClientOptions
{
    ApiKey = configuration["TypeSafe:ApiKey"],
    DefaultModel = "jev-1.13.0",
    Timeout = TimeSpan.FromSeconds(20),
    Retry = RetryPolicy.Default with { MaxRetries = 4 },
});
```

Pin a versioned model when application thresholds depend on stable behavior. The `jev-latest` alias can move.

### Per-call settings

```csharp
var result = await client.SystemOneAsync(
    state,
    questions,
    options: new RequestOptions
    {
        Timeout = TimeSpan.FromSeconds(3),
        Retry = RetryPolicy.NoRetries,
        Headers = new Dictionary<string, string> { ["X-Trace-Id"] = traceId },
    },
    cancellationToken: cancellationToken);
```

Authentication, SDK identification, `Accept`, and `Content-Type` headers cannot be overridden by custom headers.

## Retries and errors

By default, JevNet retries HTTP 408, 429, and 5xx responses, connection failures, and per-attempt timeouts. It honors `Retry-After` and `retry-after-ms` up to one minute. Caller cancellation is never retried or wrapped.

All SDK failures derive from `TypeSafeException`:

- `TypeSafeApiException` retains the status, response body, headers, endpoint, detail, and request ID.
- Status-specific types cover 400, 401, 403, 404, 422, 429, and 5xx responses.
- `TypeSafeConnectionException` and `TypeSafeTimeoutException` retain the transport cause.
- `TypeSafeResponseValidationException` reports a successful response that violates the documented schema.
- `TypeSafeConfigurationException` reports invalid local configuration before any request is sent.

```csharp
try
{
    await client.SystemOneAsync(state, questions, cancellationToken: cancellationToken);
}
catch (TypeSafeRateLimitException error)
{
    Console.Error.WriteLine($"Retry after {error.RetryAfter}; request {error.RequestId}");
}
catch (TypeSafeApiException error)
{
    Console.Error.WriteLine($"{(int)error.StatusCode}: {error.Detail}");
}
```

## Models

```csharp
var models = await client.Models.ListAsync(cancellationToken: cancellationToken);
foreach (var model in models)
{
    Console.WriteLine($"{model.Name} ({model.ReleaseDate}): {model.Description}");
}
```

## Dependency injection

```csharp
using Jev;
using Microsoft.Extensions.DependencyInjection;

services.AddTypeSafeClient(serviceProvider => new TypeSafeClientOptions
{
    ApiKey = configuration["TypeSafe:ApiKey"],
    DefaultModel = configuration["TypeSafe:Model"],
});

public sealed class TicketRouter(ITypeSafeClient client)
{
    // The registered client is a thread-safe singleton backed by IHttpClientFactory.
}
```

The returned `IHttpClientBuilder` supports the usual handler, proxy, and resilience customization.

## Compatibility

The wire contract is tested against the official JavaScript SDK 0.6.0 and Python SDK 0.7.0 with offline mock transports. These cross-SDK tests live outside this repository so the package remains a focused .NET project.

The SDK intentionally follows the current API behavior where documentation and older clients disagree: score questions require at least two non-null levels.

## Development

Requires the .NET 10 SDK listed in `global.json`; the produced packages target .NET 8.

```shell
dotnet restore --locked-mode
dotnet format JevNet.slnx --no-restore --verify-no-changes
dotnet build JevNet.slnx -c Release --no-restore
dotnet test JevNet.slnx -c Release --no-build
dotnet pack JevNet.slnx -c Release --no-build -o artifacts
```

Unit tests use in-memory HTTP handlers and never require an API key. See [CONTRIBUTING.md](CONTRIBUTING.md) for the TDD and commit conventions.

## License

MIT. See [LICENSE](LICENSE).
