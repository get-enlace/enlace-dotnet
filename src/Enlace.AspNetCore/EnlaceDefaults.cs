namespace Enlace.AspNetCore;

internal static class EnlaceDefaults
{
    public const string HttpClientName = "Enlace";

    /// <summary>
    /// Relative path, under the mount path, where the UI fetches the resolved spec.
    /// Fixed by @get-enlace/ui's client (see api/client.ts's `fetch('api/spec')`) — every
    /// adapter, in every language, serves the spec at exactly this path under its mount.
    /// </summary>
    public const string SpecEndpointPath = "/api/spec";
}
