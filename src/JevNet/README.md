# JevNet

JevNet is a typed .NET client for [TypeSafe AI](https://typesafe.ai/) and its Jev System One model, with first-class [OpenRouter](https://openrouter.ai/) support.

Turn text or structured application state into typed decisions: probabilities, labels, scores, classifications, and routing outcomes.

> JevNet is a community project. It is not affiliated with or endorsed by TypeSafe AI.

## Install

```shell
dotnet add package JevNet
```

Set `TYPESAFE_API_KEY` in the process environment, or provide an API key through `TypeSafeClientOptions`.

## Quick start

```csharp
using Jev;

using var client = new TypeSafeClient();

var result = await client.SystemOneAsync(
    new
    {
        subject = "Duplicate charge",
        message = "I was charged twice. Please refund the duplicate.",
    },
    new Dictionary<string, Question>
    {
        ["department"] = Question.Choice(
            "Which team should handle this?",
            new Dictionary<string, JevValue?>
            {
                ["billing"] = "Charges, invoices, subscriptions, and refunds",
                ["support"] = "Product questions and technical problems",
                ["other"] = null,
            }),
    });

Console.WriteLine(result.GetChoice("department").Choice);
```

All network methods are asynchronous and accept a `CancellationToken`.

## Why JevNet?

- Typed `Noul`, `Choice`, and `Score` questions and answers
- Structured state without manual JSON construction
- TypeSafe AI and OpenRouter through the same client
- Configurable timeouts, retries, cancellation, and custom headers
- Typed exceptions with status, request ID, response details, and retry information
- No runtime package dependencies in the core package

## Dependency injection

For `IHttpClientFactory` and `Microsoft.Extensions.DependencyInjection` integration:

```shell
dotnet add package JevNet.Extensions.DependencyInjection
```

## Documentation and support

- [Full documentation and examples](https://github.com/mathixu/jev-dotnet-sdk)
- [Changelog](https://github.com/mathixu/jev-dotnet-sdk/blob/main/CHANGELOG.md)
- [Report an issue](https://github.com/mathixu/jev-dotnet-sdk/issues)
- [Security policy](https://github.com/mathixu/jev-dotnet-sdk/security/policy)

Licensed under the MIT License.
