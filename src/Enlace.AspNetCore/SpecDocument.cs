using System.Text.Json.Nodes;

namespace Enlace.AspNetCore;

/// <summary>
/// Light post-processing applied to the OpenAPI document before it's handed to the UI.
/// </summary>
internal static class SpecDocument
{
    /// <summary>
    /// The UI reads its request target from the spec's own <c>servers[0].url</c> — it does
    /// no discovery of its own (see @get-enlace/ui's store/workflowStore.ts). Swashbuckle's
    /// default <c>AddSwaggerGen()</c> output doesn't emit a <c>servers</c> array at all, which
    /// would silently break the zero-config promise for the common case, so when the fetched
    /// document has no usable one we add one pointing at wherever we actually fetched the
    /// document from. Leaves an existing <c>servers</c> entry untouched.
    /// </summary>
    public static string EnsureServersUrl(string json, string fallbackBaseUrl)
    {
        var root = JsonNode.Parse(json)?.AsObject();
        if (root is null || HasUsableServer(root))
        {
            return json;
        }

        root["servers"] = new JsonArray(new JsonObject { ["url"] = fallbackBaseUrl });
        return root.ToJsonString();
    }

    private static bool HasUsableServer(JsonObject root)
    {
        if (root["servers"] is not JsonArray servers)
        {
            return false;
        }

        return servers.Any(server =>
            server is JsonObject serverObject &&
            serverObject["url"] is JsonValue urlValue &&
            urlValue.TryGetValue(out string? url) &&
            !string.IsNullOrWhiteSpace(url));
    }
}
