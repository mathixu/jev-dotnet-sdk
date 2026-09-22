using System.Net;
using System.Text.Json;

namespace Jev.Tests;

public sealed class ClientTests
{
    private const string SuccessBody = """
        {
          "model": "jev-1.13.0",
          "answers": { "urgent": { "type": "noul", "noul": 0.9 } },
          "usage": { "input_tokens": 10, "output_tokens": 1 }
        }
        """;

    [Fact]
    public void Constructor_requires_an_api_key()
    {
        var variable = Environment.GetEnvironmentVariable(TypeSafeClient.ApiKeyEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(TypeSafeClient.ApiKeyEnvironmentVariable, null);

            var error = Assert.Throws<TypeSafeConfigurationException>(() => new TypeSafeClient());

            Assert.Contains(TypeSafeClient.ApiKeyEnvironmentVariable, error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TypeSafeClient.ApiKeyEnvironmentVariable, variable);
        }
    }

    [Fact]
    public async Task SystemOneAsync_sends_the_official_request_shape_and_headers()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.OK, SuccessBody, ("x-typesafe-request-id", "req_123"));
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions
        {
            ApiKey = "  test-key  ",
            BaseUrl = "https://example.test/root/",
            DefaultModel = "jev-pinned",
            DefaultHeaders = new Dictionary<string, string> { ["X-Tenant"] = "acme" },
        });

        var response = await client.SystemOneAsync(
            "Refund this duplicate charge",
            new Dictionary<string, Question> { ["urgent"] = Question.Noul("Is this urgent?") });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://example.test/root/v1/systemone", request.Uri.AbsoluteUri);
        Assert.Equal("Bearer test-key", request.Headers["Authorization"]);
        Assert.Equal("application/json", request.Headers["Content-Type"]);
        Assert.Equal("acme", request.Headers["X-Tenant"]);
        Assert.Contains("JevNet/", request.Headers["User-Agent"], StringComparison.Ordinal);

        var body = JsonDocument.Parse(request.Body!).RootElement;
        Assert.Equal("Refund this duplicate charge", body.GetProperty("state").GetString());
        Assert.Equal("jev-pinned", body.GetProperty("model").GetString());
        Assert.Equal("noul", body.GetProperty("questions").GetProperty("urgent").GetProperty("type").GetString());
        Assert.Equal("req_123", response.RequestId);
        Assert.Equal(0.9, response.GetNoul("urgent").Noul);
    }

    [Fact]
    public async Task Request_model_and_headers_override_client_defaults()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.OK, SuccessBody);
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions
        {
            ApiKey = "test-key",
            DefaultModel = "jev-default",
            DefaultHeaders = new Dictionary<string, string> { ["X-Tenant"] = "default" },
        });
        var request = new SystemOneRequest(
            "state",
            new Dictionary<string, Question> { ["urgent"] = Question.Noul() },
            model: "jev-override");

        await client.SystemOneAsync(request, new RequestOptions
        {
            Headers = new Dictionary<string, string> { ["x-tenant"] = "override" },
        });

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal("override", recorded.Headers["x-tenant"]);
        Assert.Equal("jev-override", JsonDocument.Parse(recorded.Body!).RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task Structured_state_is_serialized_without_stringification()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.OK, SuccessBody);
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions { ApiKey = "test-key" });
        var state = JevValue.From(new { ticket = new { id = 42, text = "Help" } });

        await client.SystemOneAsync(
            state,
            new Dictionary<string, Question> { ["urgent"] = Question.Noul() });

        var body = JsonDocument.Parse(Assert.Single(handler.Requests).Body!).RootElement;
        Assert.Equal(42, body.GetProperty("state").GetProperty("ticket").GetProperty("id").GetInt32());
    }

    [Fact]
    public void Explicit_configuration_takes_precedence_over_environment()
    {
        var originalBaseUrl = Environment.GetEnvironmentVariable(TypeSafeClient.BaseUrlEnvironmentVariable);
        var originalModel = Environment.GetEnvironmentVariable(TypeSafeClient.DefaultModelEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(TypeSafeClient.BaseUrlEnvironmentVariable, "https://environment.test");
            Environment.SetEnvironmentVariable(TypeSafeClient.DefaultModelEnvironmentVariable, "environment-model");

            using var client = new TypeSafeClient(new TypeSafeClientOptions
            {
                ApiKey = "test-key",
                BaseUrl = "https://explicit.test",
                DefaultModel = "explicit-model",
            });

            Assert.Equal(new Uri("https://explicit.test"), client.BaseUrl);
            Assert.Equal("explicit-model", client.DefaultModel);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TypeSafeClient.BaseUrlEnvironmentVariable, originalBaseUrl);
            Environment.SetEnvironmentVariable(TypeSafeClient.DefaultModelEnvironmentVariable, originalModel);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("contains space")]
    [InlineData("clé")]
    public void Api_key_must_be_printable_ascii_without_whitespace(string apiKey)
    {
        Assert.Throws<TypeSafeConfigurationException>(() =>
            new TypeSafeClient(new TypeSafeClientOptions { ApiKey = apiKey }));
    }
}
