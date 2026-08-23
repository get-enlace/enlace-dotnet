using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enlace.AspNetCore;

/// <summary>Registers the middleware Enlace uses to serve its UI and resolved spec.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Mounts the Enlace UI (and the endpoint it uses to fetch the resolved OpenAPI spec)
    /// at <see cref="EnlaceOptions.MountPath"/>. Spec resolution runs once, after the host
    /// has started listening.
    /// </summary>
    public static IApplicationBuilder UseEnlace(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<EnlaceOptions>>().Value;
        var cache = app.ApplicationServices.GetRequiredService<EnlaceSpecCache>();
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        var clientFactory = app.ApplicationServices.GetRequiredService<IHttpClientFactory>();
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("Enlace");
        var server = app.ApplicationServices.GetRequiredService<IServer>();

        lifetime.ApplicationStarted.Register(() =>
            _ = ResolveOnStartupAsync(server, clientFactory, options, cache, logger));

        var embeddedProvider = new ManifestEmbeddedFileProvider(
            typeof(ApplicationBuilderExtensions).Assembly, "wwwroot-embedded");

        app.Map(options.MountPath, enlaceApp =>
        {
            enlaceApp.Use(async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments(EnlaceDefaults.SpecEndpointPath))
                {
                    await next(context);
                    return;
                }

                await cache.Ready;

                if (cache.ResolvedUrl is null)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync(
                        cache.Failure?.Message ?? "Enlace: the OpenAPI document could not be resolved.");
                    return;
                }

                // Read fresh on every request — never stored — matching the adapter contract
                // every @get-enlace/ui-compatible adapter follows.
                var client = clientFactory.CreateClient(EnlaceDefaults.HttpClientName);
                var json = await SpecResolver.TryFetchAsync(client, cache.ResolvedUrl, context.RequestAborted);

                if (json is null)
                {
                    context.Response.StatusCode = StatusCodes.Status502BadGateway;
                    await context.Response.WriteAsync(
                        $"Enlace: couldn't re-fetch the OpenAPI document from {cache.ResolvedUrl}.");
                    return;
                }

                var fallbackBaseUrl = new Uri(cache.ResolvedUrl).GetLeftPart(UriPartial.Authority);
                json = SpecDocument.EnsureServersUrl(json, fallbackBaseUrl);

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            });

            enlaceApp.UseDefaultFiles(new DefaultFilesOptions { FileProvider = embeddedProvider });
            enlaceApp.UseStaticFiles(new StaticFileOptions { FileProvider = embeddedProvider });
        });

        return app;
    }

    private static async Task ResolveOnStartupAsync(
        IServer server,
        IHttpClientFactory clientFactory,
        EnlaceOptions options,
        EnlaceSpecCache cache,
        ILogger logger)
    {
        try
        {
            var addressesFeature = server.Features.Get<IServerAddressesFeature>();
            var address = addressesFeature?.Addresses.FirstOrDefault();

            if (address is null)
            {
                throw new EnlaceConfigurationException(
                    "Enlace couldn't determine the app's own listening address to resolve the OpenAPI spec. " +
                    "Set `options.SpecUrl` in `AddEnlace()` to point at yours.");
            }

            var baseAddress = NormalizeAddress(address);

            // Logged unconditionally, ahead of spec resolution — the UI itself (index.html,
            // served via UseDefaultFiles/UseStaticFiles below) doesn't depend on the spec
            // having resolved, so this is worth knowing even if resolution fails next.
            var uiUrl = $"{baseAddress.TrimEnd('/')}/{options.MountPath.Trim('/')}/";
            logger.LogInformation("Enlace UI available at {Url}", uiUrl);

            var client = clientFactory.CreateClient(EnlaceDefaults.HttpClientName);
            var (url, _) = await SpecResolver.ResolveAsync(client, baseAddress, options, CancellationToken.None);

            cache.SetResolved(url);
            logger.LogInformation("Enlace resolved the OpenAPI spec from {Url}", url);
        }
        catch (Exception ex)
        {
            cache.SetFailed(ex);
            logger.LogCritical(ex, "Enlace failed to resolve an OpenAPI document at startup.");
        }
    }

    /// <summary>Kestrel reports wildcard-host addresses (e.g. http://[::]:5000); loop back to localhost for our own probe requests.</summary>
    private static string NormalizeAddress(string address)
    {
        var uri = new Uri(address);
        var host = uri.Host is "0.0.0.0" or "[::]" or "*" or "+" ? "localhost" : uri.Host;
        return new UriBuilder(uri) { Host = host }.Uri.ToString();
    }
}
