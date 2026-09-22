using System.Text.Json.Serialization;

namespace Jev;

/// <summary>A typed answer returned by Jev.</summary>
[JsonConverter(typeof(AnswerJsonConverter))]
public abstract class Answer
{
    private protected Answer()
    {
    }

    /// <summary>The wire discriminator for this answer.</summary>
    public abstract string Type { get; }
}
