# enlace-dotnet

ASP.NET Core adapter for [Enlace](https://github.com/get-enlace/enlace-ui) — a visual,
chained-execution canvas for any OpenAPI-documented API. This adapter's job is
intentionally small: serve the canvas UI and resolve your app's OpenAPI document.
Everything else (wiring up a chain, running it, credentials) happens client-side, in the
browser, inside `@get-enlace/ui` itself.

## What it does

- Serves the `@get-enlace/ui` static bundle at a configurable route (`/enlace` by default)
- Resolves your app's OpenAPI document automatically, or via explicit config — see
  [Spec resolution](#spec-resolution) below
- Nothing else. No server-side execution engine, no persistence (yet — see
  [Status](#status))

## Install

```bash
dotnet add package Enlace.AspNetCore
```

## Usage

```csharp
// Program.cs
builder.Services.AddEnlace();
// ...
app.UseEnlace(); // mounts at /enlace by default
```

With a customized Swashbuckle route or a different spec source entirely:

```csharp
builder.Services.AddEnlace(options =>
{
    options.SpecUrl = "https://internal-host/custom/openapi.json";
    options.MountPath = "/enlace"; // default
});
```

## Spec resolution

1. **Zero-config default** — if your app already runs Swashbuckle conventionally, its spec
   is already being served at `/swagger/v1/swagger.json`; the adapter defaults to that path
   with no configuration needed.
2. **Auto-detect fallback** — if that doesn't resolve, it tries a short list of other
   conventional paths (`/openapi.json`, `/swagger.json`) with a plain HTTP request to your
   app's own server — no reflection into route tables or framework internals.
3. **Explicit override** — set `options.SpecUrl` to point at anything else: a customized
   route, a different service's spec, a static file.
4. **Failure is loud** — if nothing resolves, startup fails with an error naming exactly
   what was tried and how to fix it, rather than rendering a silent empty canvas.

## Status

Pre-release scaffold. Persistence (saving/reloading workflows and credentials) is out of
scope for this phase — canvas state and credentials live in browser memory for the session
only.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for local development setup, build/test commands,
and how the CI/CD pipeline works.
