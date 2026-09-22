using System.Net;
using Jev;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JevNet.Extensions.DependencyInjection.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddTypeSafeClient_registers_one_shared_client_and_a_configurable_handler()
    {
        var handler = new StubHandler();
        var services = new ServiceCollection();
        services.AddTypeSafeClient(new TypeSafeClientOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://example.test",
            Retry = RetryPolicy.NoRetries,
        }).ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();

        var abstraction = provider.GetRequiredService<ITypeSafeClient>();
        var implementation = provider.GetRequiredService<TypeSafeClient>();
        var response = await abstraction.SystemOneAsync(new SystemOneRequest(
            "state",
            new Dictionary<string, Question> { ["q"] = Question.Noul() }));

        Assert.Same(implementation, abstraction);
        Assert.Equal(0.7, response.GetNoul("q").Noul);
        Assert.Equal("https://example.test/v1/systemone", handler.RequestUri?.AbsoluteUri);
    }

    [Fact]
    public void AddTypeSafeClient_can_resolve_options_from_the_service_provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ApiSettings("injected-key"));
        services.AddTypeSafeClient(provider => new TypeSafeClientOptions
        {
            ApiKey = provider.GetRequiredService<ApiSettings>().ApiKey,
        });
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ITypeSafeClient>());
    }

    private sealed record ApiSettings(string ApiKey);

    private sealed class StubHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"model\":\"jev\",\"answers\":{\"q\":{\"type\":\"noul\",\"noul\":0.7}},\"usage\":{\"input_tokens\":1,\"output_tokens\":1}}"),
            });
        }
    }
}
