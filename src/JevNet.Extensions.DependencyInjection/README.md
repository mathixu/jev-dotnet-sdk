# JevNet.Extensions.DependencyInjection

Dependency injection and `IHttpClientFactory` integration for [JevNet](https://www.nuget.org/packages/JevNet), the typed TypeSafe AI, Jev System One, and OpenRouter client.

## Install

```shell
dotnet add package JevNet.Extensions.DependencyInjection
```

The core `JevNet` package is installed automatically as a dependency.

## Register the client

```csharp
using Jev;
using Microsoft.Extensions.DependencyInjection;

services.AddTypeSafeClient(_ => new TypeSafeClientOptions
{
    ApiKey = configuration["TypeSafe:ApiKey"],
    DefaultModel = configuration["TypeSafe:Model"],
});
```

You can also omit `ApiKey` and use the `TYPESAFE_API_KEY` environment variable.

## Inject and use it

```csharp
public sealed class TicketRouter(ITypeSafeClient client)
{
    public async Task<string> RouteAsync(
        string message,
        CancellationToken cancellationToken)
    {
        var result = await client.SystemOneAsync(
            new { message },
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
            },
            cancellationToken: cancellationToken);

        return result.GetChoice("department").Choice;
    }
}
```

`AddTypeSafeClient` returns an `IHttpClientBuilder`, so the usual handler, proxy, and resilience customization remains available.

## Documentation and support

- [Full documentation and examples](https://github.com/mathixu/jev-dotnet-sdk#readme)
- [Core JevNet package](https://www.nuget.org/packages/JevNet)
- [Report an issue](https://github.com/mathixu/jev-dotnet-sdk/issues)

Licensed under the MIT License.
