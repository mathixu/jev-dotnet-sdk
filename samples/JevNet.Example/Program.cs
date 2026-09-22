using Jev;

using var client = new TypeSafeClient();

var response = await client.SystemOneAsync(
    new
    {
        ticket = new
        {
            subject = "Duplicate charge",
            message = "I was charged twice. Please refund the duplicate today.",
        },
    },
    new Dictionary<string, Question>
    {
        ["refund_requested"] = Question.Noul("Is the customer requesting a refund?"),
        ["department"] = Question.Choice(
            "Which team should handle this?",
            new Dictionary<string, JevValue?>
            {
                ["billing"] = "Payments, subscriptions, invoices, or refunds",
                ["support"] = "Product questions or technical problems",
                ["other"] = null,
            }),
        ["urgency"] = Question.Score(
            "How urgently should this be handled?",
            "Can wait",
            "Handle soon",
            "Handle now"),
    });

Console.WriteLine($"Model: {response.Model}");
Console.WriteLine($"Refund probability: {response.GetNoul("refund_requested").Noul:P0}");
Console.WriteLine($"Department: {response.GetChoice("department").Choice}");
Console.WriteLine($"Urgency: {response.GetScore("urgency").Score:F2}");
