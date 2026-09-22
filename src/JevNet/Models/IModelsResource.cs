namespace Jev;

/// <summary>Access to the Models API resource.</summary>
public interface IModelsResource
{
    /// <summary>Lists the models available to the account.</summary>
    Task<IReadOnlyList<ModelCard>> ListAsync(
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);
}
