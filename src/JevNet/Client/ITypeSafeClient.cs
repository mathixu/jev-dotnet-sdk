namespace Jev;

/// <summary>Client contract for Jev decisions through TypeSafe AI or OpenRouter.</summary>
public interface ITypeSafeClient
{
    /// <summary>Access to available-model operations.</summary>
    IModelsResource Models { get; }

    /// <summary>Evaluates named questions against text or pre-serialized state.</summary>
    Task<SystemOneResponse> SystemOneAsync(
        JevValue state,
        IReadOnlyDictionary<string, Question> questions,
        string? model = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Serializes structured .NET state and evaluates named questions against it.</summary>
    Task<SystemOneResponse> SystemOneAsync<TState>(
        TState state,
        IReadOnlyDictionary<string, Question> questions,
        string? model = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Evaluates named questions against shared state.</summary>
    Task<SystemOneResponse> SystemOneAsync(
        SystemOneRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default);
}
