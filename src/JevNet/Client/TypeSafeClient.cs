using System.Text;
using System.Text.Json;

namespace Jev;

/// <summary>An asynchronous, thread-safe client for Jev through TypeSafe AI or OpenRouter.</summary>
public sealed partial class TypeSafeClient : ITypeSafeClient, IDisposable
{
    /// <summary>The environment variable used for the API key.</summary>
    public const string ApiKeyEnvironmentVariable = "TYPESAFE_API_KEY";

    /// <summary>The environment variable used for the API root.</summary>
    public const string BaseUrlEnvironmentVariable = "TYPESAFE_BASE_URL";

    /// <summary>The environment variable used for the default model.</summary>
    public const string DefaultModelEnvironmentVariable = "TYPESAFE_DEFAULT_MODEL";

    /// <summary>The environment variable used for an OpenRouter API key.</summary>
    public const string OpenRouterApiKeyEnvironmentVariable = "OPENROUTER_API_KEY";

    /// <summary>The environment variable used for an OpenRouter API root override.</summary>
    public const string OpenRouterBaseUrlEnvironmentVariable = "OPENROUTER_BASE_URL";

    /// <summary>The environment variable used for an OpenRouter model override.</summary>
    public const string OpenRouterDefaultModelEnvironmentVariable = "OPENROUTER_DEFAULT_MODEL";

    /// <summary>The default TypeSafe API root.</summary>
    public const string DefaultBaseUrl = "https://api.typesafe.ai";

    /// <summary>The moving default Jev model alias.</summary>
    public const string DefaultModelName = "jev-latest";

    /// <summary>The default OpenRouter API root.</summary>
    public const string OpenRouterDefaultBaseUrl = "https://openrouter.ai";

    /// <summary>The moving Jev model alias on OpenRouter.</summary>
    public const string OpenRouterDefaultModelName = "~typesafe/jev-latest";

    private static readonly string Version =
        typeof(TypeSafeClient).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly TypeSafeClientSettings _settings;
    private bool _disposed;

    /// <summary>Creates a client using a private, pooled HTTP handler.</summary>
    public TypeSafeClient(TypeSafeClientOptions? options = null)
    {
        _settings = TypeSafeClientSettings.Resolve(options ?? new TypeSafeClientOptions());
        _httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _ownsHttpClient = true;
        Models = new ModelsResource(this);
    }

    /// <summary>Creates a client over a caller-owned <see cref="HttpClient"/>.</summary>
    public TypeSafeClient(HttpClient httpClient, TypeSafeClientOptions? options = null)
        : this(httpClient, options, disposeHttpClient: false) { }

    /// <summary>Creates a client and explicitly controls ownership of the supplied HTTP client.</summary>
    public TypeSafeClient(
        HttpClient httpClient,
        TypeSafeClientOptions? options,
        bool disposeHttpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _settings = TypeSafeClientSettings.Resolve(options ?? new TypeSafeClientOptions());
        _httpClient = httpClient;
        _ownsHttpClient = disposeHttpClient;
        Models = new ModelsResource(this);
    }

    /// <summary>The resolved API root.</summary>
    public Uri BaseUrl => _settings.BaseUrl;

    /// <summary>The selected Jev backend.</summary>
    public JevProvider Provider => _settings.Provider;

    /// <summary>The resolved default model.</summary>
    public string DefaultModel => _settings.DefaultModel;

    /// <summary>Access to available-model operations.</summary>
    public IModelsResource Models { get; }

    /// <summary>Evaluates named questions against shared state.</summary>
    public Task<SystemOneResponse> SystemOneAsync(
        JevValue state,
        IReadOnlyDictionary<string, Question> questions,
        string? model = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        SystemOneAsync(new SystemOneRequest(state, questions, model), options, cancellationToken);

    /// <summary>Serializes structured .NET state and evaluates named questions against it.</summary>
    public Task<SystemOneResponse> SystemOneAsync<TState>(
        TState state,
        IReadOnlyDictionary<string, Question> questions,
        string? model = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        SystemOneAsync(JevValue.From(state), questions, model, options, cancellationToken);

    /// <inheritdoc />
    public async Task<SystemOneResponse> SystemOneAsync(
        SystemOneRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        var payload = Encoding.UTF8.GetBytes(JevJson.Serialize(
            request.WithModel(request.Model ?? _settings.DefaultModel)));
        return await SendAsync(
            HttpMethod.Post,
            _settings.DecisionsPath,
            payload,
            options,
            static (_, body, headers) =>
            {
                var result = JevJson.Deserialize<SystemOneResponse>(body)
                    ?? throw new JsonException("The API returned an empty response.");
                var requestId = headers.TryGetValue("x-typesafe-request-id", out var value)
                    ? value
                    : null;
                return new SystemOneResponse(result.Model, result.Answers, result.Usage, requestId);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Disposes the internally created HTTP client, if any.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
