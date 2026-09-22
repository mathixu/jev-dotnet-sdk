using System.Collections.ObjectModel;
using System.Text.Json;

namespace Jev.Tests;

public sealed class ResponseTests
{
    private const string CompleteResponse = """
        {
          "model": "jev-1.13.0",
          "answers": {
            "urgent": { "type": "noul", "noul": 0.93 },
            "department": {
              "type": "choice",
              "choice": "billing",
              "confidence": 0.91,
              "probabilities": { "billing": 0.91, "support": 0.09 }
            },
            "severity": {
              "type": "score",
              "score": 1.7,
              "confidence": 0.84,
              "legend": { "0": "Low", "1": { "label": "Medium" }, "2": ["High"] },
              "probabilities": { "0": 0.1, "1": 0.1, "2": 0.8 }
            }
          },
          "usage": { "input_tokens": 120, "output_tokens": 12 },
          "future_field": "ignored"
        }
        """;

    [Fact]
    public void Deserialize_returns_typed_answers()
    {
        var response = JevJson.Deserialize<SystemOneResponse>(CompleteResponse)!;

        Assert.Equal("jev-1.13.0", response.Model);
        Assert.Equal(0.93, response.GetNoul("urgent").Noul);
        Assert.Equal("billing", response.GetChoice("department").Choice);
        Assert.Equal(1.7, response.GetScore("severity").Score);
        Assert.Equal("{\"label\":\"Medium\"}",
            JevJson.Serialize(response.GetScore("severity").Legend["1"]));
        Assert.Equal(132, response.Usage.TotalTokens);
    }

    [Fact]
    public void Answers_are_exposed_as_read_only_snapshots()
    {
        var response = JevJson.Deserialize<SystemOneResponse>(CompleteResponse)!;

        Assert.IsType<ReadOnlyDictionary<string, Answer>>(response.Answers);
        Assert.IsType<ReadOnlyDictionary<string, double>>(
            response.GetChoice("department").Probabilities);
    }

    [Fact]
    public void Typed_accessors_reject_an_answer_of_the_wrong_kind()
    {
        var response = JevJson.Deserialize<SystemOneResponse>(CompleteResponse)!;

        var error = Assert.Throws<InvalidOperationException>(() => response.GetScore("urgent"));

        Assert.Contains("urgent", error.Message, StringComparison.Ordinal);
        Assert.Contains("noul", error.Message, StringComparison.Ordinal);
        Assert.Contains("score", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Typed_accessors_report_an_unknown_question_name()
    {
        var response = JevJson.Deserialize<SystemOneResponse>(CompleteResponse)!;

        var error = Assert.Throws<KeyNotFoundException>(() => response.GetNoul("missing"));

        Assert.Contains("missing", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{\"answers\":{},\"usage\":{\"input_tokens\":1,\"output_tokens\":1}}", "model")]
    [InlineData("{\"model\":\"jev\",\"answers\":{},\"usage\":{\"input_tokens\":1,\"output_tokens\":1}}", "answers")]
    [InlineData("{\"model\":\"jev\",\"answers\":{\"q\":{\"type\":\"choice\",\"choice\":\"a\",\"probabilities\":{\"a\":1}}},\"usage\":{\"input_tokens\":1,\"output_tokens\":1}}", "answers.q.confidence")]
    [InlineData("{\"model\":\"jev\",\"answers\":{\"q\":{\"type\":\"future\"}},\"usage\":{\"input_tokens\":1,\"output_tokens\":1}}", "answers.q.type")]
    public void Deserialize_names_the_invalid_field(string json, string field)
    {
        var error = Assert.Throws<JsonException>(() => JevJson.Deserialize<SystemOneResponse>(json));

        Assert.Contains(field, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Response_round_trip_uses_the_official_wire_names()
    {
        var response = JevJson.Deserialize<SystemOneResponse>(CompleteResponse)!;

        var root = JsonDocument.Parse(JevJson.Serialize(response)).RootElement;

        Assert.Equal(120, root.GetProperty("usage").GetProperty("input_tokens").GetInt32());
        Assert.Equal(0.93, root.GetProperty("answers").GetProperty("urgent").GetProperty("noul").GetDouble());
        Assert.False(root.TryGetProperty("request_id", out _));
    }
}
