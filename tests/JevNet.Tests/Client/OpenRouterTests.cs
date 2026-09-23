using System.Net;
using System.Text.Json;

namespace Jev.Tests;

public sealed class OpenRouterTests
{
    private const string SuccessBody = """
        {
          "model": "typesafe/jev-1.13",
          "answers": { "urgent": { "type": "noul", "noul": 0.9 } },
          "usage": { "input_tokens": 10, "output_tokens": 1 }
        }
        """;

    [Fact]
    public async Task Provider_selects_OpenRouter_defaults_and_decisions_endpoint()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.OK, SuccessBody);
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions
        {
            Provider = JevProvider.OpenRouter,
            ApiKey = "openrouter-key",
        });

        await client.SystemOneAsync(
            "Refund this duplicate charge",
            new Dictionary<string, Question> { ["urgent"] = Question.Noul() });

        Assert.Equal(JevProvider.OpenRouter, client.Provider);
        Assert.Equal(new Uri("https://openrouter.ai"), client.BaseUrl);
        Assert.Equal("~typesafe/jev-latest", client.DefaultModel);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://openrouter.ai/api/alpha/decisions", request.Uri.AbsoluteUri);
        Assert.Equal("Bearer openrouter-key", request.Headers["Authorization"]);
        Assert.DoesNotContain("X-TypeSafe-SDK", request.Headers.Keys);
        Assert.DoesNotContain("X-TypeSafe-Runtime", request.Headers.Keys);
        Assert.Equal(
            "~typesafe/jev-latest",
            JsonDocument.Parse(request.Body!).RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public void Provider_reads_OpenRouter_environment_variables()
    {
        var originalApiKey = Environment.GetEnvironmentVariable(
            TypeSafeClient.OpenRouterApiKeyEnvironmentVariable);
        var originalBaseUrl = Environment.GetEnvironmentVariable(
            TypeSafeClient.OpenRouterBaseUrlEnvironmentVariable);
        var originalModel = Environment.GetEnvironmentVariable(
            TypeSafeClient.OpenRouterDefaultModelEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterApiKeyEnvironmentVariable,
                "environment-key");
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterBaseUrlEnvironmentVariable,
                "https://gateway.example/openrouter/");
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterDefaultModelEnvironmentVariable,
                "typesafe/jev-pinned");

            using var client = new TypeSafeClient(new TypeSafeClientOptions
            {
                Provider = JevProvider.OpenRouter,
            });

            Assert.Equal(new Uri("https://gateway.example/openrouter"), client.BaseUrl);
            Assert.Equal("typesafe/jev-pinned", client.DefaultModel);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterApiKeyEnvironmentVariable,
                originalApiKey);
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterBaseUrlEnvironmentVariable,
                originalBaseUrl);
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterDefaultModelEnvironmentVariable,
                originalModel);
        }
    }

    [Fact]
    public void Explicit_OpenRouter_configuration_takes_precedence_over_environment()
    {
        var originalBaseUrl = Environment.GetEnvironmentVariable(
            TypeSafeClient.OpenRouterBaseUrlEnvironmentVariable);
        var originalModel = Environment.GetEnvironmentVariable(
            TypeSafeClient.OpenRouterDefaultModelEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterBaseUrlEnvironmentVariable,
                "https://environment.example");
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterDefaultModelEnvironmentVariable,
                "typesafe/environment-model");

            using var client = new TypeSafeClient(new TypeSafeClientOptions
            {
                Provider = JevProvider.OpenRouter,
                ApiKey = "openrouter-key",
                BaseUrl = "https://explicit.example",
                DefaultModel = "typesafe/explicit-model",
            });

            Assert.Equal(new Uri("https://explicit.example"), client.BaseUrl);
            Assert.Equal("typesafe/explicit-model", client.DefaultModel);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterBaseUrlEnvironmentVariable,
                originalBaseUrl);
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterDefaultModelEnvironmentVariable,
                originalModel);
        }
    }

    [Fact]
    public void OpenRouter_requires_its_own_api_key_environment_variable()
    {
        var originalApiKey = Environment.GetEnvironmentVariable(
            TypeSafeClient.OpenRouterApiKeyEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(TypeSafeClient.OpenRouterApiKeyEnvironmentVariable, null);

            var error = Assert.Throws<TypeSafeConfigurationException>(() =>
                new TypeSafeClient(new TypeSafeClientOptions { Provider = JevProvider.OpenRouter }));

            Assert.Contains(
                TypeSafeClient.OpenRouterApiKeyEnvironmentVariable,
                error.Message,
                StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                TypeSafeClient.OpenRouterApiKeyEnvironmentVariable,
                originalApiKey);
        }
    }

    [Fact]
    public async Task OpenRouter_attribution_headers_can_be_configured_as_defaults()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.OK, SuccessBody);
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions
        {
            Provider = JevProvider.OpenRouter,
            ApiKey = "openrouter-key",
            DefaultHeaders = new Dictionary<string, string>
            {
                ["HTTP-Referer"] = "https://example.com",
                ["X-OpenRouter-Title"] = "Example application",
            },
        });

        await client.SystemOneAsync(
            "state",
            new Dictionary<string, Question> { ["urgent"] = Question.Noul() });

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://example.com", request.Headers["HTTP-Referer"]);
        Assert.Equal("Example application", request.Headers["X-OpenRouter-Title"]);
    }

    [Fact]
    public async Task Models_resource_maps_OpenRouter_model_metadata()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {
              "data": [
                {
                  "id": "typesafe/jev-1.13",
                  "name": "Jev 1.13",
                  "description": "A structured decision model.",
                  "created": 1789689600
                }
              ]
            }
            """);
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions
        {
            Provider = JevProvider.OpenRouter,
            ApiKey = "openrouter-key",
        });

        var models = await client.Models.ListAsync();

        var model = Assert.Single(models);
        Assert.Equal("typesafe/jev-1.13", model.Name);
        Assert.Equal("A structured decision model.", model.Description);
        Assert.Equal("2026-09-18", model.ReleaseDate);
        Assert.Equal(
            "https://openrouter.ai/api/v1/models?model_authors=typesafe",
            Assert.Single(handler.Requests).Uri.AbsoluteUri);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"data\":null}")]
    [InlineData("{\"data\":[{\"id\":\"typesafe/jev-1.13\"}]}")]
    public async Task Models_resource_rejects_invalid_OpenRouter_responses(string body)
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.OK, body);
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions
        {
            Provider = JevProvider.OpenRouter,
            ApiKey = "openrouter-key",
        });

        var error = await Assert.ThrowsAsync<TypeSafeResponseValidationException>(
            () => client.Models.ListAsync());

        Assert.Equal("GET https://openrouter.ai/api/v1/models", error.Endpoint);
    }

    [Fact]
    public void Unknown_provider_is_rejected()
    {
        var error = Assert.Throws<TypeSafeConfigurationException>(() =>
            new TypeSafeClient(new TypeSafeClientOptions
            {
                Provider = (JevProvider)int.MaxValue,
                ApiKey = "test-key",
            }));

        Assert.Contains("Provider", error.Message, StringComparison.Ordinal);
    }
}
