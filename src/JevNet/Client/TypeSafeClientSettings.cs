namespace Jev;

internal sealed class TypeSafeClientSettings
{
    private TypeSafeClientSettings(
        JevProvider provider,
        string apiKey,
        Uri baseUrl,
        string defaultModel,
        string decisionsPath,
        TimeSpan timeout,
        IReadOnlyDictionary<string, string> defaultHeaders,
        RetryPolicy retry)
    {
        Provider = provider;
        ApiKey = apiKey;
        BaseUrl = baseUrl;
        DefaultModel = defaultModel;
        DecisionsPath = decisionsPath;
        Timeout = timeout;
        DefaultHeaders = defaultHeaders;
        Retry = retry;
    }

    internal JevProvider Provider { get; }

    internal string ApiKey { get; }

    internal Uri BaseUrl { get; }

    internal string DefaultModel { get; }

    internal string DecisionsPath { get; }

    internal TimeSpan Timeout { get; }

    internal IReadOnlyDictionary<string, string> DefaultHeaders { get; }

    internal RetryPolicy Retry { get; }

    internal static TypeSafeClientSettings Resolve(TypeSafeClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var provider = ResolveProvider(options.Provider);
        var apiKey = ResolveApiKey(options.ApiKey, provider.ApiKeyEnvironmentVariable);
        var baseUrlText = Resolve(
            options.BaseUrl,
            provider.BaseUrlEnvironmentVariable,
            provider.DefaultBaseUrl).TrimEnd('/');
        if (!Uri.TryCreate(baseUrlText, UriKind.Absolute, out var baseUrl) ||
            baseUrl.Scheme is not ("http" or "https"))
        {
            throw new TypeSafeConfigurationException("BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (!string.IsNullOrEmpty(baseUrl.UserInfo))
        {
            throw new TypeSafeConfigurationException("BaseUrl must not contain credentials.");
        }

        var model = Resolve(
            options.DefaultModel,
            provider.DefaultModelEnvironmentVariable,
            provider.DefaultModel).Trim();
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new TypeSafeConfigurationException("DefaultModel cannot be empty.");
        }

        if (options.Timeout <= TimeSpan.Zero || options.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new TypeSafeConfigurationException("Timeout must be a positive, finite duration.");
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (options.DefaultHeaders is not null)
        {
            foreach (var header in options.DefaultHeaders)
            {
                headers[header.Key] = header.Value;
            }
        }

        var retry = (options.Retry ?? throw new TypeSafeConfigurationException("Retry cannot be null."))
            .Validate();
        return new TypeSafeClientSettings(
            options.Provider,
            apiKey,
            baseUrl,
            model,
            provider.DecisionsPath,
            options.Timeout,
            headers,
            retry);
    }

    private static ProviderDefaults ResolveProvider(JevProvider provider) => provider switch
    {
        JevProvider.TypeSafe => new ProviderDefaults(
            TypeSafeClient.ApiKeyEnvironmentVariable,
            TypeSafeClient.BaseUrlEnvironmentVariable,
            TypeSafeClient.DefaultModelEnvironmentVariable,
            TypeSafeClient.DefaultBaseUrl,
            TypeSafeClient.DefaultModelName,
            "/v1/systemone"),
        JevProvider.OpenRouter => new ProviderDefaults(
            TypeSafeClient.OpenRouterApiKeyEnvironmentVariable,
            TypeSafeClient.OpenRouterBaseUrlEnvironmentVariable,
            TypeSafeClient.OpenRouterDefaultModelEnvironmentVariable,
            TypeSafeClient.OpenRouterDefaultBaseUrl,
            TypeSafeClient.OpenRouterDefaultModelName,
            "/api/alpha/decisions"),
        _ => throw new TypeSafeConfigurationException($"Provider '{provider}' is not supported."),
    };

    private static string ResolveApiKey(string? explicitValue, string environmentVariable)
    {
        var key = Resolve(explicitValue, environmentVariable, string.Empty).Trim();
        if (key.Length == 0)
        {
            throw new TypeSafeConfigurationException(
                $"No API key was provided. Set TypeSafeClientOptions.ApiKey or {environmentVariable}.");
        }

        if (key.Any(character => character is < '!' or > '~'))
        {
            throw new TypeSafeConfigurationException(
                "API key must contain only printable ASCII characters without whitespace.");
        }

        return key;
    }

    private static string Resolve(string? explicitValue, string environmentVariable, string defaultValue)
    {
        if (explicitValue is not null)
        {
            return explicitValue;
        }

        var environmentValue = Environment.GetEnvironmentVariable(environmentVariable)?.Trim();
        return string.IsNullOrEmpty(environmentValue) ? defaultValue : environmentValue;
    }

    private sealed record ProviderDefaults(
        string ApiKeyEnvironmentVariable,
        string BaseUrlEnvironmentVariable,
        string DefaultModelEnvironmentVariable,
        string DefaultBaseUrl,
        string DefaultModel,
        string DecisionsPath);
}
