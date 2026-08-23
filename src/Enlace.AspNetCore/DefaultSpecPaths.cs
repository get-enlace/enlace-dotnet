namespace Enlace.AspNetCore;

/// <summary>
/// Conventional OpenAPI document paths probed at startup when <see cref="EnlaceOptions.SpecUrl"/>
/// is not set. First one that returns valid OpenAPI JSON wins.
/// </summary>
internal static class DefaultSpecPaths
{
    public const string SwashbuckleDefault = "/swagger/v1/swagger.json";
    public const string OpenApiJson = "/openapi.json";
    public const string SwaggerJson = "/swagger.json";

    public static readonly IReadOnlyList<string> All =
    [
        SwashbuckleDefault,
        OpenApiJson,
        SwaggerJson,
    ];
}
