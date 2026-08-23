using System.Net;
using Enlace.AspNetCore;
using Xunit;

namespace Enlace.AspNetCore.Tests;

public class SpecResolverTests
{
    private const string BaseAddress = "http://localhost:5000";

    [Fact]
    public async Task ResolveAsync_UsesSpecUrl_WhenConfigured()
    {
        var handler = new FakeHandler(url => url == "https://custom/openapi.json"
            ? Respond(OpenApiJson)
            : Respond(HttpStatusCode.NotFound));

        var client = new HttpClient(handler);
        var options = new EnlaceOptions { SpecUrl = "https://custom/openapi.json" };

        var (url, json) = await SpecResolver.ResolveAsync(client, BaseAddress, options, CancellationToken.None);

        Assert.Equal("https://custom/openapi.json", url);
        Assert.Equal(OpenApiJson, json);
    }

    [Fact]
    public async Task ResolveAsync_Throws_WhenSpecUrlConfigured_ButUnreachable()
    {
        var handler = new FakeHandler(_ => Respond(HttpStatusCode.NotFound));
        var client = new HttpClient(handler);
        var options = new EnlaceOptions { SpecUrl = "https://custom/openapi.json" };

        var ex = await Assert.ThrowsAsync<EnlaceConfigurationException>(
            () => SpecResolver.ResolveAsync(client, BaseAddress, options, CancellationToken.None));

        Assert.Contains("https://custom/openapi.json", ex.Message);
    }

    [Fact]
    public async Task ResolveAsync_FallsBackThroughDefaultPaths_InOrder()
    {
        var handler = new FakeHandler(url => url switch
        {
            BaseAddress + "/swagger/v1/swagger.json" => Respond(HttpStatusCode.NotFound),
            BaseAddress + "/openapi.json" => Respond(OpenApiJson),
            BaseAddress + "/swagger.json" => Respond(OpenApiJson),
            _ => Respond(HttpStatusCode.NotFound),
        });

        var client = new HttpClient(handler);
        var options = new EnlaceOptions();

        var (url, json) = await SpecResolver.ResolveAsync(client, BaseAddress, options, CancellationToken.None);

        Assert.Equal(BaseAddress + "/openapi.json", url);
        Assert.Equal(OpenApiJson, json);
    }

    [Fact]
    public async Task ResolveAsync_Throws_WithNamedPaths_WhenNothingResolves()
    {
        var handler = new FakeHandler(_ => Respond(HttpStatusCode.NotFound));
        var client = new HttpClient(handler);
        var options = new EnlaceOptions();

        var ex = await Assert.ThrowsAsync<EnlaceConfigurationException>(
            () => SpecResolver.ResolveAsync(client, BaseAddress, options, CancellationToken.None));

        foreach (var path in DefaultSpecPaths.All)
        {
            Assert.Contains(path, ex.Message);
        }

        Assert.Contains("options.SpecUrl", ex.Message);
    }

    [Fact]
    public async Task ResolveAsync_RejectsNonOpenApiJson()
    {
        var handler = new FakeHandler(_ => Respond("""{ "hello": "world" }"""));
        var client = new HttpClient(handler);
        var options = new EnlaceOptions();

        await Assert.ThrowsAsync<EnlaceConfigurationException>(
            () => SpecResolver.ResolveAsync(client, BaseAddress, options, CancellationToken.None));
    }

    private const string OpenApiJson = """{ "openapi": "3.0.1", "info": { "title": "Test", "version": "1.0" } }""";

    private static HttpResponseMessage Respond(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json) };

    private static HttpResponseMessage Respond(HttpStatusCode statusCode) => new(statusCode);

    private sealed class FakeHandler(Func<string, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request.RequestUri!.ToString()));
    }
}
