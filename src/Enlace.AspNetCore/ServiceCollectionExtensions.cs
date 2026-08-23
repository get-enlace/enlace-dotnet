using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Enlace.AspNetCore;

/// <summary>Registers the services Enlace needs (options, HTTP client, spec cache).</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Enlace. Every option has a sensible default, so
    /// <c>services.AddEnlace()</c> with no arguments works for a project already
    /// running Swashbuckle conventionally.
    /// </summary>
    public static IServiceCollection AddEnlace(this IServiceCollection services, Action<EnlaceOptions>? configure = null)
    {
        var optionsBuilder = services.AddOptions<EnlaceOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.AddHttpClient(EnlaceDefaults.HttpClientName);
        services.TryAddSingleton<EnlaceSpecCache>();

        return services;
    }
}
