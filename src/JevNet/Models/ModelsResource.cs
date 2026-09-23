using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace Jev;

/// <summary>Access to the Models API resource.</summary>
public sealed class ModelsResource : IModelsResource
{
    private readonly TypeSafeClient _client;

    internal ModelsResource(TypeSafeClient client) => _client = client;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelCard>> ListAsync(
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (_client.Provider == JevProvider.OpenRouter)
        {
            return await _client.SendAsync(
                HttpMethod.Get,
                "/api/v1/models?model_authors=typesafe",
                payload: null,
                options,
                static (_, body, _) => ReadOpenRouterModels(body),
                cancellationToken).ConfigureAwait(false);
        }

        return await _client.SendAsync(
            HttpMethod.Get,
            "/v1/models",
            payload: null,
            options,
            static (_, body, _) => ReadModels(body),
            cancellationToken).ConfigureAwait(false);
    }

    private static ReadOnlyCollection<ModelCard> ReadOpenRouterModels(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("data", out var models) ||
            models.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                "Unexpected response shape from GET /api/v1/models; expected { data: [...] }.");
        }

        var result = new List<ModelCard>();
        foreach (var item in models.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("created", out var created) ||
                !created.TryGetInt64(out var createdUnixSeconds))
            {
                throw new JsonException("Invalid response data at 'data[]'.");
            }

            string releaseDate;
            try
            {
                releaseDate = DateTimeOffset
                    .FromUnixTimeSeconds(createdUnixSeconds)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch (ArgumentOutOfRangeException error)
            {
                throw new JsonException("Invalid response data at 'data[].created'.", error);
            }

            result.Add(new ModelCard(
                RequireString(item, "id", "data[]"),
                RequireString(item, "description", "data[]"),
                releaseDate));
        }

        return new ReadOnlyCollection<ModelCard>(result);
    }

    private static ReadOnlyCollection<ModelCard> ReadModels(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("models", out var models) ||
            models.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                "Unexpected response shape from GET /v1/models; expected { models: [...] }.");
        }

        var result = new List<ModelCard>();
        foreach (var item in models.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Invalid response data at 'models[]'.");
            }

            result.Add(new ModelCard(
                RequireString(item, "name", "models[]"),
                RequireString(item, "description", "models[]"),
                RequireString(item, "release_date", "models[]")));
        }

        return new ReadOnlyCollection<ModelCard>(result);
    }

    private static string RequireString(JsonElement item, string name, string path)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Invalid response data at '{path}.{name}'.");
        }

        return value.GetString()!;
    }
}
