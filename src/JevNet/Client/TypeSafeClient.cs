using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Jev;

/// <summary>Client contract for TypeSafe AI's System One API.</summary>
public interface ITypeSafeClient
{
    /// <summary>Access to available-model operations.</summary>
    IModelsResource Models { get; }

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
        Models = new ModelsResource(this);
    }

    /// <summary>Creates a client over a caller-owned <see cref="HttpClient"/>.</summary>
    public TypeSafeClient(HttpClient httpClient, TypeSafeClientOptions? options = null)
        : this(httpClient, options, disposeHttpClient: false)
    {
    }

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

        var payload = Encoding.UTF8.GetBytes(JevJson.Serialize(
            request.WithModel(request.Model ?? _settings.DefaultModel)));
        return await SendAsync(
            HttpMethod.Post,
            "/v1/systemone",
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

    internal async Task<TResult> SendAsync<TResult>(
        HttpMethod method,
        string path,
        byte[]? payload,
        RequestOptions? options,
        Func<System.Net.HttpStatusCode, string, Dictionary<string, string>, TResult> parse,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var timeout = options?.Timeout ?? _settings.Timeout;
        if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Timeout must be positive and finite.");
        }

        var retry = (options?.Retry ?? _settings.Retry).Validate();
        var requestUrl = $"{_settings.BaseUrl.AbsoluteUri.TrimEnd('/')}{path}";
        var endpoint = $"{method.Method} {requestUrl}";

        for (var attempt = 0; ; attempt++)
        {
            using var message = CreateRequest(method, requestUrl, payload, options, attempt);
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            try
            {
                using var response = await _httpClient.SendAsync(
                    message,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutSource.Token).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(timeoutSource.Token)
                    .ConfigureAwait(false);
                var headers = SnapshotHeaders(response);

                if (!response.IsSuccessStatusCode)
                {
                    if (attempt < retry.MaxRetries && retry.StatusCodes.Contains((int)response.StatusCode))
                    {
                        await DelayBeforeRetryAsync(retry, attempt, headers, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }

                    throw TypeSafeApiExceptionFactory.Create(
                        response.StatusCode,
                        body,
                        headers,
                        endpoint);
                }

                try
                {
                    return parse(response.StatusCode, body, headers);
                }
                catch (JsonException error)
                {
                    throw new TypeSafeResponseValidationException(
                        response.StatusCode,
                        body,
                        headers,
                        endpoint,
                        error);
                }

            }
            catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
            {
                var timeoutError = new TypeSafeTimeoutException(timeout, error);
                if (attempt >= retry.MaxRetries || !retry.RetryTimeouts)
                {
                    throw timeoutError;
                }

                await DelayBeforeRetryAsync(retry, attempt, null, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException error)
            {
                var connectionError = new TypeSafeConnectionException(
                    $"Connection failed while calling {endpoint}.",
                    error);
                if (attempt >= retry.MaxRetries || !retry.RetryConnectionErrors)
                {
                    throw connectionError;
                }

                await DelayBeforeRetryAsync(retry, attempt, null, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
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

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUrl,
        byte[]? payload,
        RequestOptions? options,
        int attempt)
    {
        var message = new HttpRequestMessage(method, requestUrl);
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
        if (attempt > 0)
        {
            message.Headers.TryAddWithoutValidation("X-TypeSafe-Retry-Count", attempt.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        }

        if (payload is not null)
        {
            var content = new ByteArrayContent(payload);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            message.Content = content;
        }

        return message;
    }

    private static Dictionary<string, string> SnapshotHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(",", header.Value);
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(",", header.Value);
        }

        return headers;
    }

    private static async Task DelayBeforeRetryAsync(
        RetryPolicy policy,
        int attempt,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken)
    {
        TimeSpan? serverDelay = null;
        if (policy.RespectRetryAfter && headers is not null)
        {
            var parsed = RetryAfterParser.Parse(headers);
            if (parsed <= policy.MaximumRetryAfter)
            {
                serverDelay = parsed;
            }
        }

        var delay = serverDelay ?? Backoff(policy, attempt);
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static TimeSpan Backoff(RetryPolicy policy, int attempt)
    {
        var maximumMilliseconds = policy.BackoffMaximum.TotalMilliseconds;
        var milliseconds = policy.BackoffInitial.TotalMilliseconds * Math.Pow(2, attempt);
        if (!double.IsFinite(milliseconds) || milliseconds > maximumMilliseconds)
        {
            milliseconds = maximumMilliseconds;
        }

        if (policy.BackoffJitter > 0 && milliseconds > 0)
        {
            milliseconds *= 1 - (Random.Shared.NextDouble() * policy.BackoffJitter);
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
