using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Jev;

/// <summary>Client contract for TypeSafe AI's System One API.</summary>
public interface ITypeSafeClient
{
    /// <summary>Evaluates named questions against shared state.</summary>
    Task<SystemOneResponse> SystemOneAsync(
        SystemOneRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>An asynchronous, thread-safe client for TypeSafe AI.</summary>
public sealed class TypeSafeClient : ITypeSafeClient, IDisposable
{
    /// <summary>The environment variable used for the API key.</summary>
    public const string ApiKeyEnvironmentVariable = "TYPESAFE_API_KEY";

    /// <summary>The environment variable used for the API root.</summary>
    public const string BaseUrlEnvironmentVariable = "TYPESAFE_BASE_URL";

    /// <summary>The environment variable used for the default model.</summary>
    public const string DefaultModelEnvironmentVariable = "TYPESAFE_DEFAULT_MODEL";

    /// <summary>The default TypeSafe API root.</summary>
    public const string DefaultBaseUrl = "https://api.typesafe.ai";

    /// <summary>The moving default Jev model alias.</summary>
    public const string DefaultModelName = "jev-latest";

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
    }

    /// <summary>Creates a client over a caller-owned <see cref="HttpClient"/>.</summary>
    public TypeSafeClient(HttpClient httpClient, TypeSafeClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _settings = TypeSafeClientSettings.Resolve(options ?? new TypeSafeClientOptions());
        _httpClient = httpClient;
    }

    /// <summary>The resolved API root.</summary>
    public Uri BaseUrl => _settings.BaseUrl;

    /// <summary>The resolved default model.</summary>
    public string DefaultModel => _settings.DefaultModel;

    /// <summary>Evaluates named questions against shared state.</summary>
    public Task<SystemOneResponse> SystemOneAsync(
        JevValue state,
        IReadOnlyDictionary<string, Question> questions,
        string? model = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        SystemOneAsync(new SystemOneRequest(state, questions, model), options, cancellationToken);

    /// <inheritdoc />
    public async Task<SystemOneResponse> SystemOneAsync(
        SystemOneRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        var timeout = options?.Timeout ?? _settings.Timeout;
        if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Timeout must be positive and finite.");
        }

        using var message = CreateRequest(request.WithModel(request.Model ?? _settings.DefaultModel), options);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        using var response = await _httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            timeoutSource.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(timeoutSource.Token).ConfigureAwait(false);
        var result = JevJson.Deserialize<SystemOneResponse>(json)
            ?? throw new JsonException("The API returned an empty response.");
        var requestId = response.Headers.TryGetValues("x-typesafe-request-id", out var values)
            ? values.FirstOrDefault()
            : null;
        return new SystemOneResponse(result.Model, result.Answers, result.Usage, requestId);
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

    private HttpRequestMessage CreateRequest(SystemOneRequest request, RequestOptions? options)
    {
        var uri = new Uri($"{_settings.BaseUrl.AbsoluteUri.TrimEnd('/')}/v1/systemone");
        var message = new HttpRequestMessage(HttpMethod.Post, uri);
        var headers = new Dictionary<string, string>(_settings.DefaultHeaders, StringComparer.OrdinalIgnoreCase);
        if (options?.Headers is not null)
        {
            foreach (var header in options.Headers)
            {
                headers[header.Key] = header.Value;
            }
        }

        foreach (var header in headers)
        {
            message.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        message.Headers.TryAddWithoutValidation("User-Agent", $"JevNet/{Version}");
        message.Headers.TryAddWithoutValidation("X-TypeSafe-SDK", $"JevNet/{Version}");
        message.Headers.TryAddWithoutValidation("X-TypeSafe-Runtime", RuntimeInformation.FrameworkDescription);

        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(JevJson.Serialize(request)));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        message.Content = content;
        return message;
    }
}
