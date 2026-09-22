using System.Collections.ObjectModel;

namespace Jev;

/// <summary>Utilities for constructing named question sets.</summary>
public static class Questions
{
    /// <summary>Creates an immutable snapshot and rejects an empty request.</summary>
    public static IReadOnlyDictionary<string, Question> Create(
        IReadOnlyDictionary<string, Question> questions)
    {
        ArgumentNullException.ThrowIfNull(questions);
        if (questions.Count == 0)
        {
            throw new ArgumentException("At least one question is required.", nameof(questions));
        }

        var snapshot = new Dictionary<string, Question>(questions.Count, StringComparer.Ordinal);
        foreach (var pair in questions)
        {
            if (pair.Value is null)
            {
                throw new ArgumentException($"Question '{pair.Key}' cannot be null.", nameof(questions));
            }

            snapshot.Add(pair.Key, pair.Value);
        }

        return new ReadOnlyDictionary<string, Question>(snapshot);
    }
}
