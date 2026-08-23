# enlace-aspnet — Adapter PRD

> Scope: the ASP.NET Core adapter for Enlace. Assumes the shared `@get-enlace/ui` bundle and the overall MVP architecture (ARCHITECTURE_MVP_v3_Enlace.md) as given. This document covers only what's specific to this adapter.

## 1. What This Adapter Does

Three responsibilities, per the architecture doc — nothing more:
1. Serve the `@get-enlace/ui` static bundle at a configurable route
2. Resolve an OpenAPI document (by URL, with smart defaults — see §2) and expose it to the UI
3. Provide persistence (save/load workflows and credentials) via SQLite by default, or a user-supplied connection string

## 2. Spec Sourcing

### 2.1 Zero-config default (the common case)

Swashbuckle, the dominant ASP.NET OpenAPI toolchain, serves its generated spec at a predictable conventional path: `/swagger/v1/swagger.json`. If the consuming app already has Swashbuckle configured (the typical case, since most ASP.NET projects with Swagger UI already run it), **the adapter defaults to this path with zero configuration required.**

### 2.2 Auto-detect fallback

If the default path doesn't resolve (e.g., a customized Swashbuckle route, or a non-Swashbuckle spec source), the adapter tries a short list of other known conventional paths at startup:
- `/swagger/v1/swagger.json` (Swashbuckle default)
- `/openapi.json`
- `/swagger.json`

This is a plain HTTP request to the app's own server, exactly what a browser would do manually — not reading framework internals, not reflection into route tables. First one that returns valid OpenAPI JSON wins.

### 2.3 Explicit override

If none of the above resolve, or the user wants to point at something else entirely (a different service's spec, a static file, a non-default route), an explicit config option overrides all defaults:

```csharp
services.AddEnlace(options =>
{
    options.SpecUrl = "https://internal-host/custom/openapi.json";
});
```

### 2.4 Failure behavior

If no spec can be resolved (no default hit, no override given), fail loudly and clearly at startup — not a silent empty canvas. Error message names exactly what was tried and how to fix it:
> "Enlace couldn't find an OpenAPI document. Tried: /swagger/v1/swagger.json, /openapi.json, /swagger.json. Set `options.SpecUrl` in `AddEnlace()` to point at yours."

## 3. Serving the UI Bundle

- The adapter is a NuGet package that embeds (or fetches at build time — see open question in §7) the `@get-enlace/ui` static assets
- Registers middleware serving those assets at a configurable route, default `/enlace`
- Standard ASP.NET Core middleware registration pattern:

```csharp
// Program.cs
builder.Services.AddEnlace();
// ...
app.UseEnlace(); // default mount at /enlace
```

## 4. Persistence

**Out of scope for this phase.** No save/load for workflows or credentials. Everything (canvas state, credentials entered) lives in browser memory for the session only — consistent with the POC's original in-memory-only scope. See §8 for the explicit non-goal.

## 5. Configuration Surface (complete list for this phase)

```csharp
services.AddEnlace(options =>
{
    options.SpecUrl = null;              // null = try defaults, then auto-detect
    options.MountPath = "/enlace";        // where the UI is served
});
```

Every option has a sensible default — a user who's already running Swashbuckle conventionally should be able to call `services.AddEnlace()` and `app.UseEnlace()` with no arguments and have it work.

## 6. Success Criteria

- A project with a default Swashbuckle setup: install the NuGet package, add two lines (`AddEnlace()` / `UseEnlace()`), run the app, open `/enlace` — working canvas, spec already loaded, zero additional config
- A project with a customized spec route: same two lines plus one `SpecUrl` override
- Build and run a chain end-to-end (in-memory) against real endpoints on the running app — no save/reload expected at this stage

## 7. Resolved: How the UI Bundle Gets Embedded

Swashbuckle itself solves this exact problem, and the same mechanism applies directly:

- `Swashbuckle.AspNetCore.SwaggerUI` depends on `Microsoft.Extensions.FileProviders.Embedded` + `Microsoft.AspNetCore.StaticFiles` + `Microsoft.AspNetCore.Routing`, and bundles the compiled swagger-ui static assets directly into its own NuGet package as **embedded resources** — baked into the assembly at build time, not fetched at runtime.
- At runtime, regardless of how the files got there, `EmbeddedFileProvider` (from `Microsoft.Extensions.FileProviders.Embedded`) serves them through the standard static-files middleware. The `<EmbeddedResource Include="wwwroot-embedded/**" />` glob and the runtime serving code never need to know which of the three modes below produced the files in `wwwroot-embedded/`.

**Three distinct mechanisms populate `wwwroot-embedded/`, depending on context — see `enlace-aspnet-DEV-SETUP.md` for full detail:**

| Mode | When | Mechanism |
|---|---|---|
| **Dev proxy** | Local, actively developing `enlace-ui` itself | `app.UseEnlaceDevProxy("http://localhost:5173")` reverse-proxies to `enlace-ui`'s running Vite dev server — bypasses `wwwroot-embedded/` entirely, hot-reload, dev-only, never ships in Release |
| **Local copy script** | Local, sanity-checking the real embedded-resource path before a release | `scripts/dev-sync-ui.sh` builds `enlace-ui` locally and copies its `dist/` output straight into `wwwroot-embedded/` — manual, no registry involved |
| **CI registry fetch** | `enlace-aspnet`'s own release pipeline, at actual publish time | CI authenticates to the GitHub Packages npm registry (`https://npm.pkg.github.com/@get-enlace/ui`), resolves the tarball URL for the target version, downloads and extracts it via plain `curl`/`tar` (no `npm`/Node needed — a published npm package is just a gzipped tarball at an HTTP URL), and copies its `dist/` into `wwwroot-embedded/` before `dotnet pack` |

**Current status:** CI registry fetch is **not yet wired up** — while `@get-enlace/ui` is still churning daily during early development, automating a fetch against a registry that changes constantly is premature. Use the local copy script for now; introduce the CI step once `@get-enlace/ui` has a stable, tagged release cadence.

**Result: zero Node, zero network fetch, zero build step for the consumer**, regardless of which mode produced the embedded assets — installing the NuGet package is sufficient, exactly matching how Swashbuckle itself ships swagger-ui.

## 8. Non-Goals (this adapter, MVP)

- **No persistence in this phase.** Save/load workflows and credentials is explicitly out of scope for now — the adapter serves the UI and resolves the spec only. In-memory/session-only state is acceptable; nothing is written to disk or any database.
- No reflection into ASP.NET's route table or Swashbuckle's internal configuration objects — spec discovery is HTTP-only, per the earlier decision to avoid fragile internal-API dependencies
- No support for non-Swashbuckle spec generators beyond "any URL serving valid OpenAPI JSON" — we don't special-case NSwag, for example, beyond it also happening to work if it serves at one of the probed default paths
- No multi-tenant/per-user config — one adapter instance, one spec, per the project's established pre-prod trust model