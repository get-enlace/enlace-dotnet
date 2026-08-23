using System.Text.Json;
using Enlace.AspNetCore;
using Xunit;

namespace Enlace.AspNetCore.Tests;

public class SpecDocumentTests
{
    [Fact]
    public void EnsureServersUrl_InjectsServer_WhenMissingEntirely()
    {
        const string json = """{ "openapi": "3.0.1", "info": { "title": "Test" }, "paths": {} }""";

        var result = SpecDocument.EnsureServersUrl(json, "http://localhost:5001");

        var servers = JsonDocument.Parse(result).RootElement.GetProperty("servers");
        Assert.Equal("http://localhost:5001", servers[0].GetProperty("url").GetString());
    }

    [Fact]
    public void EnsureServersUrl_InjectsServer_WhenServersIsEmptyArray()
    {
        const string json = """{ "openapi": "3.0.1", "info": {}, "paths": {}, "servers": [] }""";

        var result = SpecDocument.EnsureServersUrl(json, "http://localhost:5001");

        var servers = JsonDocument.Parse(result).RootElement.GetProperty("servers");
        Assert.Equal(1, servers.GetArrayLength());
        Assert.Equal("http://localhost:5001", servers[0].GetProperty("url").GetString());
    }

    [Fact]
    public void EnsureServersUrl_InjectsServer_WhenExistingEntryHasNoUrl()
    {
        const string json = """{ "openapi": "3.0.1", "info": {}, "paths": {}, "servers": [{ "description": "no url here" }] }""";

        var result = SpecDocument.EnsureServersUrl(json, "http://localhost:5001");

        var servers = JsonDocument.Parse(result).RootElement.GetProperty("servers");
        Assert.Equal("http://localhost:5001", servers[0].GetProperty("url").GetString());
    }

    [Fact]
    public void EnsureServersUrl_LeavesExistingServer_Untouched()
    {
        const string json = """{ "openapi": "3.0.1", "info": {}, "paths": {}, "servers": [{ "url": "https://api.example.com" }] }""";

        var result = SpecDocument.EnsureServersUrl(json, "http://localhost:5001");

        var servers = JsonDocument.Parse(result).RootElement.GetProperty("servers");
        Assert.Equal(1, servers.GetArrayLength());
        Assert.Equal("https://api.example.com", servers[0].GetProperty("url").GetString());
    }
}
