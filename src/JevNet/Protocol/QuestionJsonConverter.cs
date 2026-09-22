using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jev;

internal sealed class QuestionJsonConverter : JsonConverter<Question>
{
    public override Question Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("Question deserialization is not part of the public wire contract.");

    public override void Write(Utf8JsonWriter writer, Question value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStartObject();
        writer.WriteString("type", value.Type);
        writer.WritePropertyName("instructions");
        WriteValue(writer, value.Instructions);

        switch (value)
        {
            case NoulQuestion { Criteria: not null } noul:
                writer.WritePropertyName("criteria");
                writer.WriteStartObject();
                if (noul.Criteria.True is not null)
                {
                    writer.WritePropertyName("true");
                    WriteValue(writer, noul.Criteria.True);
                }

                if (noul.Criteria.False is not null)
                {
                    writer.WritePropertyName("false");
                    WriteValue(writer, noul.Criteria.False);
                }

                writer.WriteEndObject();
                break;
            case ChoiceQuestion choice:
                writer.WritePropertyName("criteria");
                writer.WriteStartObject();
                foreach (var pair in choice.Criteria)
                {
                    writer.WritePropertyName(pair.Key);
                    WriteValue(writer, pair.Value);
                }

                writer.WriteEndObject();
                break;
            case ScoreQuestion score:
                writer.WritePropertyName("criteria");
                writer.WriteStartArray();
                foreach (var level in score.Criteria)
                {
                    level.WriteTo(writer);
                }

                writer.WriteEndArray();
                break;
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, JevValue? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            value.WriteTo(writer);
        }
    }
}
