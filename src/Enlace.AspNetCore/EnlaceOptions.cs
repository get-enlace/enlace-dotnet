namespace Enlace.AspNetCore;

/// <summary>
/// Configuration surface for the Enlace adapter. Every option has a sensible default —
/// a project already running Swashbuckle conventionally can call
/// <c>services.AddEnlace()</c> / <c>app.UseEnlace()</c> with no arguments.
/// </summary>
public sealed class EnlaceOptions
{
    /// <summary>
    /// Explicit URL of the OpenAPI document to load. When <c>null</c> (the default),
    /// the adapter tries <see cref="DefaultSpecPaths.All"/> against the app's own
    /// server at startup and uses the first one that returns valid OpenAPI JSON.
    /// </summary>
    public string? SpecUrl { get; set; }

    /// <summary>
    /// The route the Enlace UI is served under. Defaults to <c>/enlace</c>.
    /// </summary>
    public string MountPath { get; set; } = "/enlace";
}
