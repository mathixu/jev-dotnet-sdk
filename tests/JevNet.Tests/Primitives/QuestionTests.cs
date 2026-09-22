using System.Text.Json;

namespace Jev.Tests;

public sealed class QuestionTests
{
    [Fact]
    public void Noul_uses_the_official_wire_shape()
    {
        var question = Question.Noul();

        var json = JevJson.Serialize(question);

        Assert.Equal("{\"type\":\"noul\",\"instructions\":null}", json);
    }

    [Fact]
    public void Noul_can_describe_both_outcomes()
    {
        var question = Question.Noul(
            "Is this message spam?",
            new NoulCriteria(
                @true: "Unsolicited advertising",
                @false: "A legitimate conversation"));

        var document = JsonDocument.Parse(JevJson.Serialize(question));
        var root = document.RootElement;

        Assert.Equal("noul", root.GetProperty("type").GetString());
        Assert.Equal("Is this message spam?", root.GetProperty("instructions").GetString());
        Assert.Equal("Unsolicited advertising", root.GetProperty("criteria").GetProperty("true").GetString());
        Assert.Equal("A legitimate conversation", root.GetProperty("criteria").GetProperty("false").GetString());
    }

    [Fact]
    public void Noul_omits_an_outcome_without_a_description()
    {
        var question = Question.Noul(
            "Is this message spam?",
            new NoulCriteria(@true: "Unsolicited advertising"));

        var criteria = JsonDocument.Parse(JevJson.Serialize(question))
            .RootElement.GetProperty("criteria");

        Assert.True(criteria.TryGetProperty("true", out _));
        Assert.False(criteria.TryGetProperty("false", out _));
    }

    [Fact]
    public void Choice_preserves_labels_and_nullable_descriptions()
    {
        var question = Question.Choice(
            "Which team should handle this?",
            new Dictionary<string, JevValue?>
            {
                ["billing"] = "Charges and refunds",
                ["other"] = null,
            });

        var root = JsonDocument.Parse(JevJson.Serialize(question)).RootElement;

        Assert.Equal("choice", root.GetProperty("type").GetString());
        Assert.Equal("Charges and refunds", root.GetProperty("criteria").GetProperty("billing").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("criteria").GetProperty("other").ValueKind);
    }

    [Fact]
    public void Score_preserves_the_order_of_its_rubric()
    {
        var question = Question.Score("How urgent is this?", "Can wait", "Soon", "Now");

        var criteria = JsonDocument.Parse(JevJson.Serialize(question))
            .RootElement.GetProperty("criteria");

        Assert.Equal(new[] { "Can wait", "Soon", "Now" },
            criteria.EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public void Score_requires_at_least_two_levels()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Question.Score("How urgent is this?", "Only one"));

        Assert.Equal("criteria", error.ParamName);
    }

    [Fact]
    public void Structured_values_are_captured_as_json()
    {
        var source = new List<string> { "alpha", "beta" };
        var value = JevValue.From(source);
        source[0] = "changed";

        Assert.Equal("[\"alpha\",\"beta\"]", JevJson.Serialize(value));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    public void Values_reject_scalar_kinds_not_supported_by_the_api(string json)
    {
        var error = Assert.Throws<ArgumentException>(() => JevValue.FromJson(json));

        Assert.Contains("string, object, array, or null", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Question_collections_require_at_least_one_entry()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Questions.Create(new Dictionary<string, Question>()));

        Assert.Equal("questions", error.ParamName);
    }

    [Fact]
    public void Question_collections_are_immutable_snapshots()
    {
        var source = new Dictionary<string, Question>
        {
            ["urgent"] = Question.Noul("This is urgent"),
        };

        var questions = Questions.Create(source);
        source.Clear();

        Assert.Single(questions);
        Assert.True(questions.ContainsKey("urgent"));
    }
}
