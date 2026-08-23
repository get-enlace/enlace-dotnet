using System.Text.Json;

namespace Enlace.AspNetCore;

/// <summary>
/// Resolves the OpenAPI document: explicit <see cref="EnlaceOptions.SpecUrl"/> override
/// first, otherwise probes <see cref="DefaultSpecPaths.All"/> in order. Plain HTTP
/// requests only — no reflection into route tables or generator internals.
/// </summary>
internal static class SpecResolver
{
    public static async Task<(string Url, string Json)> ResolveAsync(
        HttpClient client,
        string baseAddress,
        EnlaceOptions options,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.SpecUrl))
        {
            var json = await TryFetchAsync(client, options.SpecUrl, cancellationToken).ConfigureAwait(false);
            if (json is not null)
            {
                return (options.SpecUrl, json);
            }

            throw new EnlaceConfigurationException(
                $"Enlace couldn't load an OpenAPI document from the configured SpecUrl: {options.SpecUrl}");
        }

        var tried = new List<string>();
        foreach (var path in DefaultSpecPaths.All)
        {
            var url = CombineUrl(baseAddress, path);
            tried.Add(path);

            var json = await TryFetchAsync(client, url, cancellationToken).ConfigureAwait(false);
            if (json is not null)
            {
                return (url, json);
            }
        }

        throw new EnlaceConfigurationException(
            $"Enlace couldn't find an OpenAPI document. Tried: {string.Join(", ", tried)}. " +
            "Set `options.SpecUrl` in `AddEnlace()` to point at yours.");
    }

    private static string CombineUrl(string baseAddress, string path) =>
        baseAddress.TrimEnd('/') + path;

    /// <summary>
    /// Fetches <paramref name="url"/> and returns its body if it's a 2xx response containing
    /// a valid OpenAPI document, or <c>null</c> otherwise. Used both to probe candidates at
    /// startup and to re-fetch the already-resolved URL fresh on each <c>/api/spec</c> request.
    /// </summary>
    internal static async Task<string?> TryFetchAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return IsValidOpenApiDocument(content) ? content : null;
        }
        catch
        {
            // Any failure (network, non-JSON body, etc.) just means this candidate didn't pan out.
            return null;
        }
    }

    private static bool IsValidOpenApiDocument(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object &&
                   (root.TryGetProperty("openapi", out _) || root.TryGetProperty("swagger", out _));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
