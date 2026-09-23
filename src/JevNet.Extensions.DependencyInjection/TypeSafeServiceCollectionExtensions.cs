using Jev;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registration helpers for JevNet.</summary>
public static class TypeSafeServiceCollectionExtensions
{
    private const string HttpClientName = "JevNet";

    /// <summary>Registers one thread-safe Jev client backed by <see cref="IHttpClientFactory"/>.</summary>
    public static IHttpClientBuilder AddTypeSafeClient(
        this IServiceCollection services,
        TypeSafeClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return services.AddTypeSafeClient(_ => options);
    }

    /// <summary>
    /// Registers one thread-safe Jev client, resolving its settings from the service provider.
    /// </summary>
    public static IHttpClientBuilder AddTypeSafeClient(
        this IServiceCollection services,
        Func<IServiceProvider, TypeSafeClientOptions> optionsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(optionsFactory);

        var builder = services.AddHttpClient(HttpClientName, static client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.TryAddSingleton(provider =>
        {
            var httpClient = provider.GetRequiredService<IHttpClientFactory>()
                .CreateClient(HttpClientName);
            var options = optionsFactory(provider)
                ?? throw new InvalidOperationException("The TypeSafe client options factory returned null.");
            return new TypeSafeClient(httpClient, options, disposeHttpClient: true);
        });
        services.TryAddSingleton<ITypeSafeClient>(provider =>
            provider.GetRequiredService<TypeSafeClient>());
        return builder;
    }
}
