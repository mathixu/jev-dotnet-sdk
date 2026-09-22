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
