namespace Jev;

/// <summary>Metadata describing a model accepted by the TypeSafe API.</summary>
public sealed class ModelCard
{
    /// <summary>Creates model metadata.</summary>
    public ModelCard(string name, string description, string releaseDate)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(releaseDate);
        Name = name;
        Description = description;
        ReleaseDate = releaseDate;
    }

    /// <summary>The model name or alias accepted in a request.</summary>
    public string Name { get; }

    /// <summary>A human-readable description.</summary>
    public string Description { get; }

    /// <summary>The release date as supplied by the API.</summary>
    public string ReleaseDate { get; }
}
