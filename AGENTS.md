# Agent instructions

See `../AGENTS.md` for workspace-level conventions (git workflow, writing voice, pre-commit, secrets, etc). This file covers only what's specific to this repo.

---

## EcoTelemetry

OpenTelemetry o11y mod for Eco. Public repo on GitHub.

## Build

```bash
dotnet restore EcoTelemetry.csproj
dotnet build EcoTelemetry.csproj -c Release
```

The csproj targets `net10.0` (matches `EcoServerTargetFramework` in current Eco) and pulls `Eco.ReferenceAssemblies` from NuGet for type-check. Real validation only happens when the DLL is deployed onto a running server.

## Public-repo discipline

This repo is **public**. Do not cite filenames or line numbers from `StrangeLoopGames/Eco` (proprietary game source) in any committed file. Knowledge from the source is fine. Pointers into it are not. Anchor any references in:

- <https://wiki.play.eco/en/Modding>
- <https://docs.play.eco/>
- <https://github.com/StrangeLoopGames/EcoModKit>

Same rule applies to private sibling repos under `coilysiren/` (`eco-mods`, `eco-configs`, `eco-mods-assets`, `eco-mods-assets-embeded`).

## Mod packaging shape

Eco loads precompiled mods from `Mods/<ModName>/` at server startup. EcoTelemetry ships as a single ZIP containing:

- `EcoTelemetry.dll`
- All transitive OpenTelemetry NuGet DLLs (the build copies them to `bin/Release/net10.0/`)
- `EcoTelemetry.example.json` (copied to the server's `Configs/` folder by the operator)

The release workflow in `.github/workflows/release.yml` produces this ZIP.

## Roadmap

v1 (this repo right now): exception capture via OTLP logs.

Next:
- Metrics worker (player count, sim tick time, GC, threadpool, web request rate)
- Trace surface (plugin init spans, slow handler detection, web request hooks)
- IConfigurablePlugin support so admins can edit config from the Eco web UI
- mod.io publication

---

## Post-push follow-up (auto-schedule)

Per the workspace "Default to proactive scheduling" rule: after pushing to `main`, schedule a wake-up to verify the build + release + on-host install landed on kai-server. CI builds the .NET assembly, the deploy step ssh-installs into `/home/kai/Steam/steamapps/common/EcoServer/Mods/UserCode/EcoTelemetry/` (no sudo, NoSudo path), then the running Eco server picks up the mod on next reload.

- **Cadence**: 360s after push. Build is small; install is an scp + script run.
- **Verify CI**: `coily gh run list --repo coilysiren/eco-telemetry --limit 1` should show `completed/success`. Re-schedule once at +180s if in progress; stop on failure.
- **Verify install**: `coily ssh kai-server ls /home/kai/Steam/steamapps/common/EcoServer/Mods/UserCode/EcoTelemetry/` should show the freshly written assembly. Compare mtime to the push time; mismatched mtime means the deploy didn't run.
- **Eco server reload**: the mod takes effect after the running Eco server reloads (server restart or in-game `/reload` by an admin). Don't trigger that automatically; just note in the wake-up summary that a reload is pending if the assembly is fresh but telemetry hasn't updated.
