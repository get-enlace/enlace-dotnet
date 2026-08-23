# Contributing to enlace-dotnet

## Layout

- `src/Enlace.AspNetCore/` — the adapter, packaged as the `Enlace.AspNetCore` NuGet package
- `tests/Enlace.AspNetCore.Tests/` — unit tests
- `scripts/dev-sync-ui.sh` — local dev: builds `@get-enlace/ui` from a checkout and copies its
  `dist/` into `wwwroot-embedded/`
- `scripts/ci-fetch-ui.sh` — CI: fetches `@get-enlace/ui`'s published tarball, from GitHub
  Packages (dev) or npmjs.org (prod), and does the same (curl + tar, no Node)
- `ui-version.txt` — the pinned `@get-enlace/ui` version `deploy-prod` embeds; updated by
  `handle-ui-release` (see CI/CD below) whenever `enlace-ui` publishes a real prod release

## Build & test

```bash
dotnet build
dotnet test
```

## Local development against `@get-enlace/ui`

`wwwroot-embedded/` (the adapter's embedded static assets) is never committed — it's a
build artifact, populated one of two ways:

- **`scripts/dev-sync-ui.sh [path-to-enlace-ui-checkout]`** — builds `@get-enlace/ui` from a
  local checkout and copies its `dist/` output in, so you can sanity-check the real
  embedded-resource path before a release without touching the registry.
- **CI** (`scripts/ci-fetch-ui.sh`) — fetches the published tarball instead; see CI/CD below.

## CI/CD

Mirrors [`enlace-js`](https://github.com/get-enlace/enlace-js)'s per-package workflow
pattern.

- `.github/workflows/build.yml` — build + test on every PR into `main`.
- `.github/workflows/enlace-aspnetcore.yml` — two triggers: push to `main` (path-filtered to
  `src/Enlace.AspNetCore/**`, `ui-version.txt`, etc.), and `repository_dispatch:
  enlace-ui-release`, fired by `enlace-ui`'s own release workflow whenever it publishes (see
  [`release-strategy.md`](https://github.com/get-enlace/enlace-ui)) — this repo is an
  **embedding-based** adapter, so it must actively rebuild and republish on every
  `enlace-ui` change, or consumers stay frozen on an old bundle. No manual
  `workflow_dispatch` escape hatch — a forced rebuild is just a commit (even a trivial one)
  pushed to `main`.

  `handle-ui-release` only runs for a *production* dispatch (a dev dispatch is a no-op here —
  see `deploy-dev` below) and does exactly one thing: pins the incoming version into
  `ui-version.txt` and commits it — a `GITHUB_TOKEN`-authored push can't retrigger the
  workflow, so it doesn't try to.

  `deploy-dev` runs **unconditionally** on every trigger — push, or a `repository_dispatch`
  for either environment — always fetching whatever's currently under `@get-enlace/ui`'s
  floating `dev` dist-tag, no version to resolve or pin. It packs a `<Version>-dev.<run id>`
  build and publishes it to GitHub Packages, then tags it (`enlace-aspnetcore-v*`). It's
  deliberately not gated on `handle-ui-release` in any way — a `development` dispatch needs
  to trigger a real rebuild here too, or this repo (an embedding-based adapter) would only
  ever pick up a fresh dev UI build whenever its own `main` happened to get pushed to,
  silently defeating the whole point of the dispatch cascade.

  `deploy-prod` (gated behind the `production` environment's required-reviewer approval)
  packs whatever version is currently committed in the `.csproj`, fetches the UI bundle
  pinned in `ui-version.txt` from npmjs.org (not GitHub Packages — this is the prod path),
  publishes to nuget.org, tags the release, and commits the next patch version bump.

  Publishing to nuget.org uses **Trusted Publishing** (OIDC) rather than a stored API key —
  `NuGet/login@v1` exchanges this run's GitHub OIDC token for a short-lived (1hr) NuGet API
  key at publish time. Nothing long-lived is stored in this repo; the trust relationship
  lives entirely in the Trusted Publisher policy configured on nuget.org's side.

One-time setup this needs:
- **On GitHub** (**Settings → Environments**): a `development` environment and a
  `production` environment (the latter with a required reviewer).
- **On GitHub** (`production` environment secret): `NUGET_USER` — your nuget.org username
  (the profile name, *not* an email address) — passed to `NuGet/login@v1`.
  `GITHUB_TOKEN` (used for the GitHub Packages dev channel) is automatic — no setup needed.
- **On nuget.org** (Trusted Publishing → Add policy): Package owner = the account/org that
  owns `Enlace.AspNetCore`; Repository = `get-enlace/enlace-dotnet`; Workflow file =
  `enlace-aspnetcore.yml`; Environment = `production` — this policy is what actually
  authorizes the OIDC exchange; the workflow's `id-token: write` permission alone isn't
  enough without it.
- No secret is needed here for `repository_dispatch` itself — the PAT that sends it
  (`CROSS_REPO_PAT`) lives on `enlace-ui`'s side, not this repo's.
