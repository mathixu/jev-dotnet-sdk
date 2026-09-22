using System.Collections.ObjectModel;
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
        CancellationToken cancellationToken = default) =>
        await _client.SendAsync(
            HttpMethod.Get,
            "/v1/models",
            payload: null,
            options,
            static (_, body, _) => ReadModels(body),
            cancellationToken).ConfigureAwait(false);

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
                RequireString(item, "name"),
                RequireString(item, "description"),
                RequireString(item, "release_date")));
        }

        return new ReadOnlyCollection<ModelCard>(result);
    }

    private static string RequireString(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Invalid response data at 'models[].{name}'.");
        }

        return value.GetString()!;
    }
}
