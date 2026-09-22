using System.Collections.ObjectModel;
using System.Net;

namespace Jev.Tests;

public sealed class ModelsTests
{
    [Fact]
    public async Task ListAsync_gets_and_decodes_available_models()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.OK, """
            {
              "models": [
                {
                  "name": "jev-latest",
                  "description": "Latest stable Jev model",
                  "release_date": "2026-09-15",
                  "future_field": true
                }
              ]
            }
            """);
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions { ApiKey = "test-key" });

        var models = await client.Models.ListAsync();

        var model = Assert.Single(models);
        Assert.Equal("jev-latest", model.Name);
        Assert.Equal("Latest stable Jev model", model.Description);
        Assert.Equal("2026-09-15", model.ReleaseDate);
        Assert.IsType<ReadOnlyCollection<ModelCard>>(models);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.typesafe.ai/v1/models", request.Uri.AbsoluteUri);
        Assert.Null(request.Body);
        Assert.False(request.Headers.ContainsKey("Content-Type"));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"models\":null}")]
    [InlineData("{\"models\":{}}")]
    [InlineData("{\"models\":[{\"name\":\"jev\"}]}")]
    public async Task ListAsync_rejects_an_invalid_response_shape(string body)
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.OK, body);
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions { ApiKey = "test-key" });

        var error = await Assert.ThrowsAsync<TypeSafeResponseValidationException>(
            () => client.Models.ListAsync());

        Assert.Equal(HttpStatusCode.OK, error.StatusCode);
    }

    [Fact]
    public async Task ListAsync_uses_the_shared_retry_pipeline()
    {
        var handler = new RecordingHandler();
        handler.Enqueue(HttpStatusCode.ServiceUnavailable, "busy");
        handler.Enqueue(HttpStatusCode.OK, "{\"models\":[]}");
        using var httpClient = new HttpClient(handler);
        using var client = new TypeSafeClient(httpClient, new TypeSafeClientOptions
        {
            ApiKey = "test-key",
            Retry = RetryPolicy.Default with { BackoffInitial = TimeSpan.Zero, BackoffJitter = 0 },
        });

        var models = await client.Models.ListAsync();

        Assert.Empty(models);
        Assert.Equal(2, handler.Requests.Count);
    }
}
