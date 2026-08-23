# enlace-dotnet

ASP.NET Core adapter for [Enlace](../enlace-ui). See [`PRD.md`](PRD.md) for the full spec.

## Layout

- `src/Enlace.AspNetCore/` — the adapter, packaged as the `Enlace.AspNetCore` NuGet package
- `tests/Enlace.AspNetCore.Tests/` — unit tests
- `scripts/dev-sync-ui.sh` — local dev: builds `@get-enlace/ui` from a checkout and copies its
  `dist/` into `wwwroot-embedded/`
- `scripts/ci-fetch-ui.sh` — CI: fetches `@get-enlace/ui`'s published tarball, from GitHub
  Packages (dev) or npmjs.org (prod), and does the same (curl + tar, no Node)
- `ui-version.txt` — the pinned `@get-enlace/ui` version `deploy-prod` embeds; updated by
  `handle-ui-release` (see CI/CD below) whenever `enlace-ui` publishes a real prod release

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

## Build & test

```bash
dotnet build
dotnet test
```

## CI/CD

Mirrors [`enlace-js`](../enlace-js)'s per-package workflow pattern.

- `.github/workflows/build.yml` — build + test on every PR into `main`.
- `.github/workflows/enlace-aspnetcore.yml` — two triggers: push to `main` (path-filtered to
  `src/Enlace.AspNetCore/**`, `ui-version.txt`, etc.), and `repository_dispatch:
  enlace-ui-release`, fired by `enlace-ui`'s own release workflow whenever it publishes (see
  [`release-strategy.md`](../release-strategy.md)) — this repo is an **embedding-based**
  adapter (§2), so it must actively rebuild and republish on every `enlace-ui` change, or
  consumers stay frozen on an old bundle. No manual `workflow_dispatch` escape hatch — a
  forced rebuild is just a commit (even a trivial one) pushed to `main`.

  `handle-ui-release` only runs for a *production* dispatch (a dev dispatch is a no-op here —
  see `deploy-dev` below) and does exactly one thing: pins the incoming version into
  `ui-version.txt` and commits it. It then falls through, in the same run, into `deploy-dev`
  and `deploy-prod` — rather than relying on that commit's push to retrigger the workflow,
  which a `GITHUB_TOKEN`-authored push can't do anyway.

  `deploy-dev` always fetches whatever's currently under `@get-enlace/ui`'s floating `dev`
  dist-tag — no version to resolve or pin, since that tag always points at the latest dev
  build regardless of its underlying version number. It packs a `<Version>-dev.<run id>`
  build and publishes it to GitHub Packages, then tags it (`enlace-aspnetcore-v*`).

  `deploy-prod` (gated behind the `production` environment's required-reviewer approval)
  packs whatever version is currently committed in the `.csproj`, fetches the UI bundle
  pinned in `ui-version.txt` from npmjs.org (not GitHub Packages — this is the prod path),
  publishes to nuget.org, tags the release, and commits the next patch version bump.

One-time setup this needs, done in the repo's GitHub settings, not in code:
- A `development` environment and a `production` environment (the latter with a required
  reviewer) under **Settings → Environments**.
- A `NUGET_API_KEY` secret (an nuget.org API key) on the `production` environment.
  `GITHUB_TOKEN` (used for the GitHub Packages dev channel) is automatic — no setup needed.
- No secret is needed here for `repository_dispatch` itself — the PAT that sends it
  (`CROSS_REPO_PAT`) lives on `enlace-ui`'s side, not this repo's.

## Status

Pre-release scaffold. Persistence is out of scope for this phase.
