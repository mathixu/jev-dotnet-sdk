namespace Jev;

internal sealed class TypeSafeClientSettings
{
    private TypeSafeClientSettings(
        string apiKey,
        Uri baseUrl,
        string defaultModel,
        TimeSpan timeout,
        IReadOnlyDictionary<string, string> defaultHeaders,
        RetryPolicy retry)
    {
        ApiKey = apiKey;
        BaseUrl = baseUrl;
        DefaultModel = defaultModel;
        Timeout = timeout;
        DefaultHeaders = defaultHeaders;
        Retry = retry;
    }

    internal string ApiKey { get; }

    internal Uri BaseUrl { get; }

    internal string DefaultModel { get; }

    internal TimeSpan Timeout { get; }

    internal IReadOnlyDictionary<string, string> DefaultHeaders { get; }

    internal RetryPolicy Retry { get; }

    internal static TypeSafeClientSettings Resolve(TypeSafeClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var apiKey = ResolveApiKey(options.ApiKey);
        var baseUrlText = Resolve(
            options.BaseUrl,
            TypeSafeClient.BaseUrlEnvironmentVariable,
            TypeSafeClient.DefaultBaseUrl).TrimEnd('/');
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
            TypeSafeClient.DefaultModelEnvironmentVariable,
            TypeSafeClient.DefaultModelName);
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
        return new TypeSafeClientSettings(apiKey, baseUrl, model, options.Timeout, headers, retry);
    }

    private static string ResolveApiKey(string? explicitValue)
    {
        var key = Resolve(explicitValue, TypeSafeClient.ApiKeyEnvironmentVariable, string.Empty).Trim();
        if (key.Length == 0)
        {
            throw new TypeSafeConfigurationException(
                $"No API key was provided. Set TypeSafeClientOptions.ApiKey or {TypeSafeClient.ApiKeyEnvironmentVariable}.");
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
}
