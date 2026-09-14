# enlace-dotnet

ASP.NET Core adapter for [Enlace](https://github.com/get-enlace/enlace) — an interactive visual execution graph for any OpenAPI 3.x API.

[![NuGet](https://img.shields.io/nuget/v/Enlace.AspNetCore.svg)](https://www.nuget.org/packages/Enlace.AspNetCore)
[![Live Demo](https://img.shields.io/badge/demo-live%20on%20render-success)](https://enlace-fastapi.onrender.com/enlace/)
[![Star on GitHub](https://img.shields.io/github/stars/get-enlace/enlace?style=social)](https://github.com/get-enlace/enlace)

This adapter's job is intentionally small: serve the canvas UI and resolve your app's OpenAPI document. Everything else (wiring up a chain, concurrent execution, credentials) happens client-side in the browser inside `@get-enlace/ui`. Full documentation: [get-enlace.github.io](https://get-enlace.github.io/).

## What it does

- Serves the `@get-enlace/ui` static bundle at a configurable route (`/enlace` by default)
- Resolves your app's OpenAPI document automatically, or via explicit config — see [Spec resolution](#spec-resolution) below
- Client-side execution: All chain execution, Kahn's algorithm DAG concurrency, and credential handling run directly in the browser via `@get-enlace/ui`.

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

## Architecture

The ASP.NET Core adapter is intentionally thin and symmetric across Enlace languages: it serves static assets and resolves your OpenAPI document. All workflow execution happens client-side directly from your browser to your API endpoints. Workflows and layout are autosaved locally in the browser via IndexedDB.

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for local development setup, build/test commands,
and how the CI/CD pipeline works.
